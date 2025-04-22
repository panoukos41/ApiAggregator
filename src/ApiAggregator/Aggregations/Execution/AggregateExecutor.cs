using ApiAggregator.Aggregations.Internal;
using ApiAggregator.Common;
using Microsoft.Extensions.Caching.Hybrid;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApiAggregator.Aggregations.Execution;

public sealed class AggregateExecutor<TRequest, TResponse> where TResponse : notnull
{
    private readonly AggregateExecutor executor;
    private readonly Aggregate<TRequest, TResponse> aggregate;

    public AggregateExecutor(AggregateExecutor executor, Aggregate<TRequest, TResponse> aggregate)
    {
        this.executor = executor;
        this.aggregate = aggregate;
    }

    public Task<Result<TResponse>> Execute(TRequest request, CancellationToken cancellationToken = default)
    {
        return executor.Execute(aggregate, request, cancellationToken);
    }
}

public sealed class AggregateExecutor
{
    private readonly IServiceProvider serviceProvider;
    private readonly HybridCache cache;

    public AggregateExecutor(IServiceProvider serviceProvider, HybridCache cache, ILogger<AggregateExecutor> logger)
    {
        this.serviceProvider = serviceProvider;
        this.cache = cache;
    }

    public async Task<Result<TResponse>> Execute<TRequest, TResponse>(Aggregate<TRequest, TResponse> aggregate, TRequest request, CancellationToken cancellationToken = default) where TResponse : notnull
    {
        using var results = new ResultPool(aggregate.Calls.Length);
        try
        {
            if (aggregate.Parallel)
            {
                var all = await Task.WhenAll(aggregate.Calls.Select(call => Execute(this, call, request, cancellationToken)));
                for (int i = 0; i < all.Length; i++)
                {
                    results[i] = all[i];
                }
            }
            else
            {
                for (int i = 0; i < aggregate.Calls.Length; i++)
                {
                    var call = aggregate.Calls[i];
                    results[i] = await Execute(this, call, request, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            return Problems.InternalServerError with
            {
                Detail = ex.Message
            };
        }

        // join results
        var response = new JsonObject();
        foreach (var (i, result) in results.All().Index())
        {
            var call = aggregate.Calls[i];
            response[call.Name] = result.DeepClone();
        }

        // convert to response object
        var final = response.Deserialize<TResponse>();
        return final is { } ? final : Problems.InternalServerError with
        {
            Detail = $"Could create object of type {typeof(TResponse)}"
        };

        static async Task<JsonObject> Execute(AggregateExecutor executor, AggregateCall<TRequest, TResponse> call, TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var serviceProvider = executor.serviceProvider;
                var cache = executor.cache;

                var cacheKey = call.Cache?.Invoke(request);
                var task = cacheKey is { Length: > 0 }
                    ? cache.GetOrCreateAsync(cacheKey, (serviceProvider, call, request), static (state, ct) => state.call.Execute(state.serviceProvider, state.request, ct), cancellationToken: cancellationToken)
                    : call.Execute(executor.serviceProvider, request, cancellationToken);
                return await task;
            }
            catch (Exception ex)
            {
                if (call.Fallback is { })
                {
                    var fallback = await call.Fallback(executor.serviceProvider, request, ex);
                    if (fallback is { })
                    {
                        return fallback;
                    }
                }
                throw;
            }
        }
    }

    private readonly struct ResultPool : IDisposable
    {
        private readonly int length;
        private readonly JsonObject[] array;

        public JsonObject this[int index]
        {
            get => array[index];
            set => array[index] = value;
        }

        public ResultPool(int length)
        {
            this.length = length;
            array = ArrayPool<JsonObject>.Shared.Rent(length);
        }

        public IEnumerable<JsonObject> All()
        {
            for (int i = 0; i < length; i++)
            {
                yield return array[i];
            }
        }

        public void Dispose()
        {
            ArrayPool<JsonObject>.Shared.Return(array, true);
        }
    }
}

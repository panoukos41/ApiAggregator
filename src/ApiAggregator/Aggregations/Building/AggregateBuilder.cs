using ApiAggregator.Aggregations.Execution;
using ApiAggregator.Aggregations.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json.Nodes;

namespace ApiAggregator.Aggregations.Building;

public static class AggregateBuilder
{
    public static AggregateBuilder<TRequest, TResponse> Create<TRequest, TResponse>() where TResponse : notnull
    {
        return new AggregateBuilder<TRequest, TResponse>();
    }

    public static IServiceCollection AddAggregate<TRequest, TResponse>(this IServiceCollection services, Action<AggregateBuilder<TRequest, TResponse>> builder)
        where TResponse : notnull
    {
        var b = Create<TRequest, TResponse>();
        builder(b);

        services.AddSingleton(b.Build());
        services.AddScoped<AggregateExecutor<TRequest, TResponse>>();
        services.TryAddScoped<AggregateExecutor>();
        return services;
    }
}

public sealed class AggregateBuilder<TRequest, TResponse> where TResponse : notnull
{
    private readonly List<AggregateCall<TRequest, TResponse>> calls = [];
    private bool parallel;

    public AggregateBuilder<TRequest, TResponse> AddFunctionCall(
        string name,
        Func<TRequest, JsonObject> call,
        Func<TRequest, string?>? cache = null,
        TimeSpan? cacheDuration = null,
        Func<IServiceProvider, TRequest, Exception, ValueTask<JsonObject?>>? fallback = null)
    {
        calls.Add(new AggregateFunctionCall<TRequest, TResponse>(call)
        {
            Name = name,
            Cache = cache,
            CacheDuration = cacheDuration,
            Fallback = fallback,
        });
        return this;
    }

    public AggregateBuilder<TRequest, TResponse> AddServiceCall<TService>(
        string name,
        Func<TService, TRequest, CancellationToken, ValueTask<JsonObject>> call,
        Func<TRequest, string?>? cache = null,
        TimeSpan? cacheDuration = null,
        Func<IServiceProvider, TRequest, Exception, ValueTask<JsonObject?>>? fallback = null)
        where TService : notnull
    {
        calls.Add(new AggregateServiceCall<TService, TRequest, TResponse>(call)
        {
            Name = name,
            Cache = cache,
            CacheDuration = cacheDuration,
            Fallback = fallback
        });
        return this;
    }

    public AggregateBuilder<TRequest, TResponse> ExecuteInParallel(bool parallel = true)
    {
        this.parallel = parallel;
        return this;
    }

    public Aggregate<TRequest, TResponse> Build() => new()
    {
        Calls = [.. calls],
        Parallel = parallel,
    };
}

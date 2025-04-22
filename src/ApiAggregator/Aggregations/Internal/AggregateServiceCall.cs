using System.Text.Json.Nodes;

namespace ApiAggregator.Aggregations.Internal;

public sealed class AggregateServiceCall<TService, TRequest, TResponse> : AggregateCall<TRequest, TResponse> where TService : notnull
{
    private readonly Func<TService, TRequest, CancellationToken, ValueTask<JsonObject>> call;

    public AggregateServiceCall(Func<TService, TRequest, CancellationToken, ValueTask<JsonObject>> call)
    {
        this.call = call;
    }

    public override ValueTask<JsonObject> Execute(IServiceProvider serviceProvider, TRequest request, CancellationToken cancellationToken)
    {
        var service = serviceProvider.GetRequiredService<TService>();
        return call(service, request, cancellationToken);
    }
}

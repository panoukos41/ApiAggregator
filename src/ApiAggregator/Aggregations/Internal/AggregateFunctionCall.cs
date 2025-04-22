using System.Text.Json.Nodes;

namespace ApiAggregator.Aggregations.Internal;

public sealed class AggregateFunctionCall<TRequest, TResponse> : AggregateCall<TRequest, TResponse>
{
    private readonly Func<TRequest, JsonObject> call;

    public AggregateFunctionCall(Func<TRequest, JsonObject> call)
    {
        this.call = call;
    }

    public override ValueTask<JsonObject> Execute(IServiceProvider serviceProvider, TRequest request, CancellationToken cancellationToken)
    {
        var r = call(request);
        return new(r);
    }
}

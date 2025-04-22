using System.Text.Json.Nodes;

namespace ApiAggregator.Aggregations.Internal;

public abstract class AggregateCall<TRequest, TResponse>
{
    public required string Name { get; init; }

    public Func<TRequest, string?>? Cache { get; init; }

    public TimeSpan? CacheDuration { get; init; }

    public Func<IServiceProvider, TRequest, Exception, ValueTask<JsonObject?>>? Fallback { get; init; }

    public abstract ValueTask<JsonObject> Execute(IServiceProvider serviceProvider, TRequest request, CancellationToken cancellationToken);
}

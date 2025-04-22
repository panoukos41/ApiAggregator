namespace ApiAggregator.Aggregations.Internal;

public sealed class Aggregate<TRequest, TResponse>
{
    public bool Parallel { get; init; }

    public required AggregateCall<TRequest, TResponse>[] Calls { get; init; }
}

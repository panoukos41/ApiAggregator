namespace ApiAggregator.Features.Statistics.Models;

public sealed class Performance
{
    public required string Tag { get; init; }

    public required TimeSpan AverageTime { get; init; }

    public required string Bucket { get; init; }
}
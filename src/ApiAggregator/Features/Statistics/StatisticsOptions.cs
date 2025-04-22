using ApiAggregator.Features.Statistics.Models;

namespace ApiAggregator.Features.Statistics;

public class StatisticsOptions
{
    public TimeSpan ReportInterval { get; init; } = TimeSpan.FromMinutes(15);

    public PerformanceBucket[] Buckets { get; init; } = [];

    public static readonly StatisticsOptions Default = new()
    {
        Buckets = [
            new() { Name = "fast", MinTime = TimeSpan.Zero, MaxTime = TimeSpan.FromMilliseconds(100) },
            new() { Name = "average", MinTime = TimeSpan.FromMilliseconds(100), MaxTime = TimeSpan.FromMilliseconds(200) },
            new() { Name = "slow", MinTime = TimeSpan.FromMilliseconds(200), MaxTime = null },
        ]
    };
}

using ApiAggregator.Features.Statistics.Models;

namespace ApiAggregator.Test.Unit.Statistics;

public sealed class PerformanceBucketTests
{
    private static readonly PerformanceBucket[] Buckets = [
        new() { Name = "fast", MinTime = TimeSpan.Zero, MaxTime = TimeSpan.FromMilliseconds(100) },
        new() { Name = "average", MinTime = TimeSpan.FromMilliseconds(100), MaxTime = TimeSpan.FromMilliseconds(200) },
        new() { Name = "slow", MinTime = TimeSpan.FromMilliseconds(200), MaxTime = null },
    ];

    private static PerformanceBucket? GetBucket(TimeSpan time) => Buckets.FirstOrDefault(x => x.IsInBucket(time));

    [Test]
    [Arguments("fast", 0)]
    [Arguments("fast", 50)]
    [Arguments("fast", 99)]
    [Arguments("average", 100)]
    [Arguments("average", 150)]
    [Arguments("average", 199)]
    [Arguments("slow", 200)]
    [Arguments("slow", 250)]
    [Arguments("slow", 299)]
    [Arguments("slow", 301)]
    public async Task Should_Be_In_Bucket(string expectedBucket, int timeInMs)
    {
        var time = TimeSpan.FromMilliseconds(timeInMs);

        var bucket = GetBucket(time);

        await Assert.That(bucket?.Name).IsEqualTo(expectedBucket);
    }
}

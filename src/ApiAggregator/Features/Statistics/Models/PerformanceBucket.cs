namespace ApiAggregator.Features.Statistics.Models;

public sealed class PerformanceBucket
{
    public required string Name { get; init; }

    /// <summary>
    /// The minimum time to qualify for this bucket. This value is inclusive.
    /// eg: For time = 50ms and minTime = 50ms, time qualifies for this bucket.
    /// </summary>
    public required TimeSpan MinTime { get; init; }

    /// <summary>
    /// The maximum value to be at this bucket. This value is exclusive.
    /// eg: For time = 100ms and maxTime = 100ms, time does not qualify for this bucket.
    /// </summary>
    public required TimeSpan? MaxTime { get; init; }

    /// <summary>
    /// Returns if a time can be included in this bucket.
    /// </summary>
    /// <param name="time">The time to check if it fits this bucket.</param>
    /// <returns><see langword="true"/> for Time greater or equal to MinTime and lesser than MaxTime  otherwise <see langword="false"/>.</returns>
    public bool IsInBucket(TimeSpan time)
    {
        return time >= MinTime && (MaxTime is null || time < MaxTime);
    }
}

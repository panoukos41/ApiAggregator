using System.Diagnostics;

namespace ApiAggregator.Features.Statistics.Models;

[DebuggerDisplay("{CalculateTime()}")]
public readonly struct Measurement
{
    public long StartTimestamp { get; }

    public long EndTimestamp { get; }

    public Measurement(long startTimestamp, long endTimestamp)
    {
        StartTimestamp = startTimestamp;
        EndTimestamp = endTimestamp;
    }

    public readonly TimeSpan CalculateTime()
    {
        return TimeProvider.System.GetElapsedTime(StartTimestamp, EndTimestamp);
    }
}

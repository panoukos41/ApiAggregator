using ApiAggregator.Common;
using ApiAggregator.Features.Statistics.Models;
using System.Collections.Concurrent;

namespace ApiAggregator.Features.Statistics;

public sealed class StatisticsService
{
    private readonly TimeProvider timeProvider;
    private readonly StatisticsOptions options;
    private readonly ConcurrentDictionary<string, ConcurrentBag<Measurement>> statistics = [];

    public IEnumerable<IGrouping<string, Measurement>> Statistics => statistics.Select(kv => Grouping.Create(kv.Key, kv.Value.ToArray())); // use ToArray to capture the collection as it is at this time.

    public StatisticsService(TimeProvider timeProvider, StatisticsOptions? options = null)
    {
        this.timeProvider = timeProvider;
        this.options = options ?? StatisticsOptions.Default;
    }

    public Measurer StartMeasuring(string tag)
    {
        statistics.TryAdd(tag, []);
        return new Measurer(tag, this);
    }

    public void Add(string tag, long startTimestamp, long endTimestamp)
    {
        var stats = statistics.GetOrAdd(tag, []);
        stats.Add(new(startTimestamp, endTimestamp));
    }

    public IEnumerable<Performance> CalculatePerformances(DateTimeOffset? after = null)
    {
        return Statistics.Select(group =>
        {
            var average = after is { }
                ? group.Where(x => x.StartTimestamp >= after.Value.Ticks).Average(x => x.CalculateTime().Ticks)
                : group.Average(x => x.CalculateTime().Ticks);

            var averageTimespan = TimeSpan.FromTicks((long)average);
            var bucket = options.Buckets.FirstOrDefault(b => b.IsInBucket(averageTimespan));
            return new Performance
            {
                Tag = group.Key,
                AverageTime = averageTimespan,
                Bucket = bucket?.Name ?? "unknown"
            };
        });
    }

    public sealed class Measurer : IDisposable
    {
        private readonly string tag;
        private readonly StatisticsService service;
        private readonly long startTimestamp;
        private bool canceled;

        public Measurer(string tag, StatisticsService service)
        {
            this.tag = tag;
            this.service = service;
            startTimestamp = service.timeProvider.GetTimestamp();
        }

        public void Cancel()
        {
            canceled = true;
        }

        public void Stop()
        {
            if (canceled) return;

            service.Add(tag, startTimestamp, service.timeProvider.GetTimestamp());
            Cancel();
        }

        public void Dispose()
        {
            if (canceled) return;

            service.Add(tag, startTimestamp, service.timeProvider.GetTimestamp());
        }
    }
}

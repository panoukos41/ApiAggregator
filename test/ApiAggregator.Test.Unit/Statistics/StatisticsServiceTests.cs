using ApiAggregator.Features.Statistics;
using Microsoft.Extensions.Time.Testing;
using System;

namespace ApiAggregator.Test.Unit.Statistics;

public sealed class StatisticsServiceTests : TestBase
{
    [Test]
    public async Task Should_Take_Correct_Measurement()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new StatisticsService(timeProvider);

        using (var m1 = services.StartMeasuring("test"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(50));
        }

        var measurementGroup = services.Statistics.First();
        var measurement = measurementGroup.First();

        await Assert.That(measurementGroup.Key).IsEqualTo("test");
        await Assert.That(measurementGroup).HasCount(1);
        await Assert.That(measurement.CalculateTime()).IsEqualTo(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public async Task Should_Run_In_Parallel()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new StatisticsService(timeProvider);

        Parallel.For(0, 100, _ =>
        {
            using var m1 = services.StartMeasuring("test");
        });

        var statistics = services.Statistics.ToArray();
        var measurementGroup = statistics[0];

        await Assert.That(statistics).HasCount(1);
        await Assert.That(measurementGroup.Key).IsEqualTo("test");
        await Assert.That(measurementGroup).HasCount(100);
        await Assert.That(measurementGroup.All(x => x.CalculateTime() == TimeSpan.FromMilliseconds(0))).IsTrue();
    }

    [Test]
    public async Task Should_Calculate_Correct_Performances()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new StatisticsService(timeProvider); // uses default options with default buckets (fast < 100ms, average 100ms - 200ms, slow > 200ms);

        using (var m1 = services.StartMeasuring("fast"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(99));
        }
        using (var m1 = services.StartMeasuring("average"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        }
        using (var m1 = services.StartMeasuring("slow"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        }

        var performances = services.CalculatePerformances();
        foreach (var perf in performances)
        {
            await Assert.That(perf.Bucket).IsNotNull();
            await Assert.That(perf.Bucket).IsNotEqualTo("unknown");
            await Assert.That(perf.Bucket).IsEqualTo(perf.Tag);
        }
    }

    [Test]
    public async Task Should_Calculate_Correct_Performances_After_Given_Time()
    {
        var timeProvider = new FakeTimeProvider();
        var services = new StatisticsService(timeProvider); // uses default options with default buckets (fast < 100ms, average 100ms - 200ms, slow > 200ms);

        var now = timeProvider.GetUtcNow();

        using (var m1 = services.StartMeasuring("fast"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(99));
        }
        using (var m1 = services.StartMeasuring("average"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        }
        using (var m1 = services.StartMeasuring("slow"))
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        }

        var performances = services.CalculatePerformances(now);
        foreach (var perf in performances)
        {
            await Assert.That(perf.Bucket).IsNotNull();
            await Assert.That(perf.Bucket).IsNotEqualTo("unknown");
            await Assert.That(perf.Bucket).IsEqualTo(perf.Tag);
        }
    }
}

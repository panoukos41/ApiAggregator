namespace ApiAggregator.Features.Statistics;

public sealed class StatisticsHostedService : IHostedService, IAsyncDisposable
{
    private readonly TimeProvider timeProvider;
    private readonly StatisticsService statisticsService;
    private readonly ILogger<StatisticsHostedService> logger;
    private readonly StatisticsOptions options;

    private ITimer? timer;

    public StatisticsHostedService(TimeProvider timeProvider, StatisticsService statisticsService, ILogger<StatisticsHostedService> logger, StatisticsOptions? options = null)
    {
        this.timeProvider = timeProvider;
        this.statisticsService = statisticsService;
        this.logger = logger;
        this.options = options ?? StatisticsOptions.Default;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = timeProvider.CreateTimer(LogPerformances, null, options.ReportInterval, options.ReportInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        return Task.CompletedTask;
    }

    private void LogPerformances(object? state)
    {
        var allTimePerf = statisticsService.CalculatePerformances();
        var lastFiveMinutes = statisticsService.CalculatePerformances(timeProvider.GetUtcNow().AddMinutes(-5));

        var anomalies = allTimePerf
            .Join(lastFiveMinutes, p => p.Tag, p => p.Tag, (all, last) => new { tag = all.Tag, all, last })
            .Where(perfs => perfs.last.AverageTime.TotalMilliseconds > perfs.all.AverageTime.TotalMilliseconds * 1.5);

        foreach (var anomaly in anomalies)
        {
            logger.LogInformation("Statistic: Performance anomaly for {tag}. The last 5 minutes average has been {}ms compared to the all time average of {}ms", anomaly.tag, anomaly.last.AverageTime.TotalMilliseconds, anomaly.all.AverageTime.TotalMilliseconds);
        }
    }

    public ValueTask DisposeAsync()
    {
        return timer?.DisposeAsync() ?? new();
    }
}

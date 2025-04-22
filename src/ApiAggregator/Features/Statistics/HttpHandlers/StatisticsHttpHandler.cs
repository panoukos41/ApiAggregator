namespace ApiAggregator.Features.Statistics.HttpHandlers;

public sealed class StatisticsHttpHandler : DelegatingHandler
{
    private readonly StatisticsService statisticsService;

    public StatisticsHttpHandler(StatisticsService statisticsService)
    {
        this.statisticsService = statisticsService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var domain = request.RequestUri!.DnsSafeHost;

        using var measurement = statisticsService.StartMeasuring(domain);
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch
        {
            measurement.Cancel();
            throw;
        }
    }
}

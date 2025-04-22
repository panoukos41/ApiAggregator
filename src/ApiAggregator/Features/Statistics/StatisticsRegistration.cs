using ApiAggregator.Features.Statistics;
using ApiAggregator.Features.Statistics.HttpHandlers;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.DependencyInjection;

public static class StatisticsRegistration
{
    public static void AddStatisticsFeature(this IServiceCollection services)
    {
        services.AddSingleton<StatisticsService>();
        services.AddHostedService<StatisticsHostedService>();

        services.AddTransient<StatisticsHttpHandler>();
        services.ConfigureAll<HttpClientFactoryOptions>(options =>
        {
            options.HttpMessageHandlerBuilderActions.Add(b =>
            {
                var handler = b.Services.GetRequiredService<StatisticsHttpHandler>();
                b.AdditionalHandlers.Add(handler);
            });
        });
    }

    public static void UseStatisticsFeature(this WebApplication app)
    {
        app.MapGet("/api/statistics", (StatisticsService statistics) => statistics.CalculatePerformances()).RequireAuthorization();
    }
}

using ApiAggregator.Aggregations.Building;
using ApiAggregator.Aggregations.Execution;
using ApiAggregator.Clients.NewsApi;
using ApiAggregator.Clients.OpenWeatherMap;
using ApiAggregator.Clients.TheCatApi;
using ApiAggregator.Common;
using ApiAggregator.Features.Dashboard.Requests;
using System.Text.Json.Nodes;

namespace Microsoft.Extensions.DependencyInjection;

public static class DashboardRegistration
{
    public static void AddDashboardFeature(this IServiceCollection services)
    {
        services.AddAggregate<GetDashboard, JsonObject>(b => b
            .ExecuteInParallel()
            .AddServiceCall<NewsApiClient>(
                name: "news",
                call: (client, request, ct) => request.News is { } news ? new(client.TopHeadlines(news, ct)) : new([]),
                cache: r => r.News is { } news ? $"news::country-{news.Country}:q-{news.Query}" : null,
                cacheDuration: TimeSpan.FromHours(2)
            )
            .AddServiceCall<OpenWeatherMapClient>(
                name: "weather",
                call: (client, request, ct) => request.Weather is { } weather ? new(client.Weather(weather, ct)) : new([]),
                cache: r => r.Weather is { } weather ? $"weather::lat-{weather.Latitude}:lon-{weather.Longitude}" : null,
                cacheDuration: TimeSpan.FromHours(2)
            )
            .AddServiceCall<TheCatApiClient>(
                name: "cat",
                call: (client, request, ct) => request.Cat is { } cat ? new(client.GetImage(cat, ct)) : new([])
            )
        );
    }

    public static void UseDashboardFeatures(this WebApplication app)
    {
        app.MapGet("/api/dashboard", (
            [AsParameters] NewsApiTopHeadlinesQuery news,
            [AsParameters] OpenWeatherMapWeatherQuery weather,
            [AsParameters] TheCatApiGetImageQuery cat,
            AggregateExecutor<GetDashboard, JsonObject> executor, CancellationToken ct) =>
        {
            var request = new GetDashboard
            {
                News = news,
                Weather = weather,
                Cat = cat,
            };
            return executor.Execute(request, ct).ToOk();
        });
    }
}

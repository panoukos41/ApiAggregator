using ApiAggregator.Clients.NewsApi;
using ApiAggregator.Clients.OpenWeatherMap;
using ApiAggregator.Clients.TheCatApi;

namespace ApiAggregator.Features.Dashboard.Requests;

public sealed record GetDashboard
{
    public NewsApiTopHeadlinesQuery? News { get; init; }

    public OpenWeatherMapWeatherQuery? Weather { get; init; }

    public TheCatApiGetImageQuery? Cat { get; init; }
}

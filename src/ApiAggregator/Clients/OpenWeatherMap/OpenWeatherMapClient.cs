using Flurl.Http;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace ApiAggregator.Clients.OpenWeatherMap;

public sealed record OpenWeatherMapClientOptions
{
    public string Url { get; init; } = "https://api.openweathermap.org";

    public required string AppId { get; init; }
}

public sealed record OpenWeatherMapClientOptionsValidator : IValidateOptions<OpenWeatherMapClientOptions>
{
    public ValidateOptionsResult Validate(string? name, OpenWeatherMapClientOptions options)
    {
        return string.IsNullOrEmpty(options.AppId)
            ? ValidateOptionsResult.Fail("OpenWeatherMapClientOptions AppId must be provided")
            : ValidateOptionsResult.Success;
    }
}

public sealed class OpenWeatherMapClient : ClientBase<OpenWeatherMapClientOptions>
{
    public OpenWeatherMapClient(HttpClient httpClient, IOptionsSnapshot<OpenWeatherMapClientOptions> options) : base(httpClient, options)
    {
    }

    public Task<JsonObject> Weather(OpenWeatherMapWeatherQuery query, CancellationToken cancellationToken = default)
    {
        return Client
            .Request(Options.Url)
            .AppendPathSegment("/data/2.5/weather")
            .SetQueryParam("lat", query.Latitude.ToString())
            .SetQueryParam("lon", query.Longitude.ToString())
            .SetQueryParam("appid", Options.AppId)
            .GetJsonAsync<JsonObject>(cancellationToken: cancellationToken);
    }
}

public sealed class OpenWeatherMapWeatherQuery
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }
}

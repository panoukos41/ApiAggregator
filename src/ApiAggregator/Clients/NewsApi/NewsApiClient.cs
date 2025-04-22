using Flurl.Http;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace ApiAggregator.Clients.NewsApi;

public sealed record NewsApiClientOptions
{
    public string Url { get; init; } = "https://newsapi.org";

    public required string ApiKey { get; init; }
}

public sealed record NewsApiClientOptionsValidator : IValidateOptions<NewsApiClientOptions>
{
    public ValidateOptionsResult Validate(string? name, NewsApiClientOptions options)
    {
        return string.IsNullOrEmpty(options.ApiKey)
            ? ValidateOptionsResult.Fail("NewsApiClient ApiKey must be provided")
            : ValidateOptionsResult.Success;
    }
}

public sealed class NewsApiClient : ClientBase<NewsApiClientOptions>
{
    public NewsApiClient(HttpClient httpClient, IOptionsSnapshot<NewsApiClientOptions> options) : base(httpClient, options)
    {
    }

    public Task<JsonObject> TopHeadlines(NewsApiTopHeadlinesQuery query, CancellationToken cancellationToken = default)
    {
        return Client
            .Request(Options.Url)
            .AppendPathSegment("/v2/top-headlines")
            .WithHeader("X-Api-Key", Options.ApiKey)
            .WithHeader("User-Agent", "testing")
            .SetQueryParam("q", query?.Query)
            .SetQueryParam("country", query?.Country)
            .GetJsonAsync<JsonObject>(cancellationToken: cancellationToken);
    }
}

public sealed class NewsApiTopHeadlinesQuery
{
    public required string Country { get; init; }

    public string? Query { get; init; }
}

using Flurl.Http;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace ApiAggregator.Clients.TheCatApi;

public sealed record TheCatApiClientOptions
{
    public string Url { get; init; } = "https://api.thecatapi.com";

    public required string ApiKey { get; init; }
}

public sealed record TheCatApiClientOptionsValidator : IValidateOptions<TheCatApiClientOptions>
{
    public ValidateOptionsResult Validate(string? name, TheCatApiClientOptions options)
    {
        return string.IsNullOrEmpty(options.ApiKey)
            ? ValidateOptionsResult.Fail("TheCatApiClientOptions ApiKey must be provided")
            : ValidateOptionsResult.Success;
    }
}

public sealed class TheCatApiClient : ClientBase<TheCatApiClientOptions>
{
    public TheCatApiClient(HttpClient httpClient, IOptionsSnapshot<TheCatApiClientOptions> options) : base(httpClient, options)
    {
    }

    public async Task<JsonObject> GetImage(TheCatApiGetImageQuery? query = null, CancellationToken cancellationToken = default)
    {
        var results = await Client
            .Request(Options.Url)
            .AppendPathSegment("v1/images/search")
            .WithHeader("X-Api-Key", Options.ApiKey)
            .SetQueryParam("breed_ids", query?.Breeds?.Length is > 0 ? string.Join(',', query.Breeds) : null)
            .GetJsonAsync<JsonArray>(cancellationToken: cancellationToken);

        return results[0]!.AsObject();
    }
}

public sealed class TheCatApiGetImageQuery
{
    public string[]? Breeds { get; init; }
}

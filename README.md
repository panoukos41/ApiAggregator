# Api Aggregator

Sample app to test an aggregation service that can be used to aggregate multiple calls together preferably other external APIs.

## The Project

The project is built around the idea of Feature folders.

The root of the project contains building blocks that are to be used to create each feature. Right now the project contains the `Clients` folder to keep external API clients together, the `Common` folder to include some common models and abstractions and the `Aggregations` folder that provides the ability to build multiple aggregates.

### Aggregations

The implementation is made up of the one builder, an executor and the aggregate itself that has been created using the builder. More builders can be created for complex scenarios. The aggregate itself consists of a list of aggregate calls and options the runner can use. Aggregates are typed by their request/response objects and the executor takes care to synthesize the final response object. All calls inherit from the base `AggregateCall` which is a simple interface that allows to implement different ways of executing code. The default implementations are of `AggregateFunctionCall` and `AggregateServiceCall`. The former was used mostly for testing but it allows the calling of a simple function while the latter uses the service provider to resolve the service you want and passes it to your provided function. You can also combine the as everything is an `AggregateCall`. For both implementations callbacks were used to make it easy to call your desired function.

All aggregates by default support caching and fallback behavior. Below is the implemented GetDashboard call registered in the service provider:
```csharp
// Aggregates are defined by their request/response objects.
services.AddAggregate<GetDashboard, JsonObject>(b => b
    .ExecuteInParallel() // Indicate to the executor we want to run in parallel
    .AddServiceCall<NewsApiClient>( // Call the news api, cache response for request with the same country and query.
        name: "news",
        call: (client, request, ct) => request.News is { } news ? new(client.TopHeadlines(news, ct)) : new([]),
        cache: r => r.News is { } news ? $"news::country-{news.Country}:q-{news.Query}" : null,
        cacheDuration: TimeSpan.FromHours(2)
    )
    .AddServiceCall<OpenWeatherMapClient>( // Call the weather api and cache response for requests with the same latitude and longitude.
        name: "weather",
        call: (client, request, ct) => request.Weather is { } weather ? new(client.Weather(weather, ct)) : new([]),
        cache: r => r.Weather is { } weather ? $"weather::lat-{weather.Latitude}:lon-{weather.Longitude}" : null,
        cacheDuration: TimeSpan.FromHours(2)
    )
    .AddServiceCall<TheCatApiClient>( // Call the cat api to get a new image every time no need for caching.
        name: "cat",
        call: (client, request, ct) => request.Cat is { } cat ? new(client.GetImage(cat, ct)) : new([])
    )
);
```

To execute the aggregate we can inject the `AggregateExecutor` and call execute passing the aggregate and the request object or we can inject the `AggregateExecuter<TRequest, TResponse`> as in the dashboard minimal api:
```csharp

// Due to limitation of the minimal api break the original GetDashboard object into 3.
app.MapGet("/api/dashboard", (
    [AsParameters] NewsApiTopHeadlinesQuery news,
    [AsParameters] OpenWeatherMapWeatherQuery weather,
    [AsParameters] TheCatApiGetImageQuery cat,
    // Inject the executor for the request/response combo and call execute!
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
```

### Statistics
The project also contains a simple statistics feature that gathers performance measurements for all external API calls. This is done through a delegation handler which is attached to all HttpClients through the HttpClientFactory. In case we have an api that is not using the HttpClients factory we can use the `StatisticsService` itself to measure as is done in the delegation handler:

```csharp
protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
    var domain = request.RequestUri!.DnsSafeHost;

    // start the time measurement, on dispose the time is calculated.
    using var measurement = statisticsService.StartMeasuring(domain);
    try
    {
        return await base.SendAsync(request, cancellationToken);
    }
    catch
    {
        // cancel the measurement as we don't want to include faulty calls.
        measurement.Cancel();
        throw;
    }
}
```

The statistics implementation also provides an endpoint to see the current performance for all of the external services and a hosted service that monitors for anomalies. More on the configuration in the running section.

## Running The Project

### Run

To run the project you will need Visual Studio or Visual Studio Code and .NET 9.0 installed.

You need to provide API keys for the [News](https://newsapi.org/), [Weather](https://openweathermap.org/api), and [Cat](https://thecatapi.com/) APIs in your appsettings or user secrets like this:
```jsonc
// root app settings object
{
  // Client keys
  "Clients": {
    "NewsApi": {
      "ApiKey": "<your api key>"
    },
    "OpenWeatherMap": {
      "AppId": "<your app id>"
    },
    "TheCatApi": {
      "ApiKey": "<your api key>"
    }
  }
}
```

The project has no other external dependencies so just hit run and navigate to [https://localhost:5001/docs](https://localhost:5001/docs) to see the Scalar UI where you can execute both calls.

### Test
The tests are based on the [TUnit]() library which requires you to enable in Visual Studio the preview feature of `Microsoft.Testing.Platform`.

After that you can rebuild the project and the tests will show up.

Alternatively you can just `dotnet test` for the tests to run in a console environment.



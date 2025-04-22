using ApiAggregator.Clients.NewsApi;
using ApiAggregator.Clients.OpenWeatherMap;
using ApiAggregator.Clients.TheCatApi;
using Scalar.AspNetCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddHybridCache();
services.AddOpenApi();
services.AddAuthentication()
    .AddBearerToken(options =>
    {
        options.Events.OnMessageReceived = (ctx) =>
        {
            ctx.Principal = new ClaimsPrincipal([new ClaimsIdentity("test")]);
            ctx.Success();
            return Task.CompletedTask;
        };
    });
services.AddAuthorization();

// News Client
services.AddHttpClient<NewsApiClient>().AddStandardResilienceHandler();
services.AddOptionsWithValidateOnStart<NewsApiClientOptions, NewsApiClientOptionsValidator>().BindConfiguration("Clients:NewsApi");

// Open Weather Client
services.AddHttpClient<OpenWeatherMapClient>().AddStandardResilienceHandler(); ;
services.AddOptionsWithValidateOnStart<OpenWeatherMapClientOptions, OpenWeatherMapClientOptionsValidator>().BindConfiguration("Clients:OpenWeatherMap");

// The Cat Api Client
services.AddHttpClient<TheCatApiClient>().AddStandardResilienceHandler();
services.AddOptionsWithValidateOnStart<TheCatApiClientOptions, TheCatApiClientOptionsValidator>().BindConfiguration("Clients:TheCatApi");

// Features
services.AddDashboardFeature();
services.AddStatisticsFeature();

var app = builder.Build();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("docs", options =>
    {
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.Http);
        options.WithHttpBearerAuthentication(options =>
        {
            options.Token = "";
        });
    });
}

app.UseDashboardFeatures();
app.UseStatisticsFeature();

app.Run();

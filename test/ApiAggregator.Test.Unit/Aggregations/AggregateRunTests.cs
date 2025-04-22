using ApiAggregator.Aggregations.Building;
using ApiAggregator.Aggregations.Execution;
using ApiAggregator.Aggregations.Internal;
using ApiAggregator.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using TUnit.Assertions.AssertConditions.Throws;

namespace ApiAggregator.Test.Unit.Aggregations;

public sealed class AggregateRunTests : TestBase
{
    [Test]
    public async Task Should_Execute()
    {
        var services = CreateProvider(services => services.AddScoped<AggregateExecutor>());
        var execLogs = new List<string>();

        var aggregate = new Aggregate<JsonObject, JsonObject>
        {
            Calls = [
                new AggregateFunctionCall<JsonObject, JsonObject>(r => { execLogs.Add("1"); return r; }) { Name = "1" },
                new AggregateFunctionCall<JsonObject, JsonObject>(r => { execLogs.Add("2"); return r; }) { Name = "2" },
            ]
        };

        var executor = services.GetRequiredService<AggregateExecutor>();
        var r = await executor.Execute(aggregate, [], default);

        await Assert.That(execLogs).Contains("1");
        await Assert.That(execLogs).Contains("2");
    }

    [Test]
    public async Task Should_Build()
    {
        var services = CreateProvider(services => services.AddScoped<AggregateExecutor>());
        var execLogs = new List<string>();

        var aggregate = AggregateBuilder
            .Create<JsonObject, JsonObject>()
            .AddFunctionCall("1", r => { execLogs.Add("1"); return r; })
            .AddFunctionCall("2", r => { execLogs.Add("2"); return r; })
            .Build();

        var executor = services.GetRequiredService<AggregateExecutor>();
        var r = await executor.Execute(aggregate, [], default);

        await Assert.That(execLogs).Contains("1");
        await Assert.That(execLogs).Contains("2");
    }

    private sealed class TestService
    {
        private readonly ILogger<TestService> logger;

        public TestService(ILogger<TestService> logger)
        {
            this.logger = logger;
        }

        public async ValueTask<JsonObject> Exec(TimeSpan? delay = null, Action? action = null)
        {
            logger.LogInformation("Executing TestService with delay {delay}", delay?.ToString() ?? "0");
            if (delay is not null)
            {
                await Task.Delay(delay.Value);
            }
            action?.Invoke();
            return [];
        }
    }

    [Test]
    public async Task Should_Register()
    {
        var execLogs = new List<string>();
        var services = CreateProvider(services =>
        {
            services.AddSingleton<TestService>();

            services.AddAggregate<JsonObject, JsonObject>(b => b
                .AddFunctionCall("1", r => { execLogs.Add("1"); return r; })
                .AddServiceCall<TestService>("2", (service, request, ct) => { execLogs.Add("2"); return service.Exec(); })
            );
        });

        var executor = services.GetRequiredService<AggregateExecutor<JsonObject, JsonObject>>();
        var r = await executor.Execute([], default);

        await Assert.That(execLogs).Contains("1");
        await Assert.That(execLogs).Contains("2");
    }

    [Test]
    public async Task Should_Run_In_Parallel()
    {
        var execLogs = new List<string>();
        var services = CreateProvider(services =>
        {
            services.AddSingleton<TestService>();

            services.AddAggregate<JsonObject, JsonObject>(b => b
                .ExecuteInParallel()
                .AddServiceCall<TestService>("1", (service, request, ct) => service.Exec(TimeSpan.FromMilliseconds(50), () => execLogs.Add("1")))
                .AddServiceCall<TestService>("2", (service, request, ct) => service.Exec(TimeSpan.FromMilliseconds(10), () => execLogs.Add("2")))
            );
        });

        var executor = services.GetRequiredService<AggregateExecutor<JsonObject, JsonObject>>();
        var r = await executor.Execute([], default);

        await Assert.That(execLogs).Contains("1");
        await Assert.That(execLogs).Contains("2");
        await Assert.That(execLogs[0]).IsEqualTo("2");
    }

    [Test]
    public async Task Should_Cache()
    {
        var calls = 0;
        var request = new JsonObject
        {
            { "name", JsonValue.Create("test") }
        };
        var services = CreateProvider(services =>
        {
            services.AddSingleton<TestService>();

            services.AddAggregate<JsonObject, JsonObject>(b => b
                .ExecuteInParallel()
                .AddServiceCall<TestService>(
                    name: "test",
                    call: (service, request, ct) => service.Exec(TimeSpan.FromMilliseconds(50), () => Interlocked.Increment(ref calls)),
                    cache: request => request.TryGetPropertyValue("name", out var prop) ? prop?.ToString() : null
                )
            );
        });

        var executor = services.GetRequiredService<AggregateExecutor<JsonObject, JsonObject>>();
        var r1 = await executor.Execute(request, default);
        var r2 = await executor.Execute(request, default);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task Should_Use_Fallback()
    {
        var request = new JsonObject
        {
            { "name", JsonValue.Create("test") }
        };
        var services = CreateProvider(services =>
        {
            services.AddSingleton<TestService>();

            services.AddAggregate<JsonObject, JsonObject>(b => b
                .ExecuteInParallel()
                .AddServiceCall<TestService>(
                    name: "test",
                    call: (service, request, ct) => throw new Exception("failed"),
                    fallback: (sp, request, ex) => new([])
                )
            );
        });

        var executor = services.GetRequiredService<AggregateExecutor<JsonObject, JsonObject>>();
        var r1 = await executor.Execute(request, default);

        await Assert.That(r1).IsTypeOf<Result<JsonObject>.Ok>();
    }

    [Test]
    public async Task Should_Use_Fallback_But_Ignore()
    {
        var request = new JsonObject
        {
            { "name", JsonValue.Create("test") }
        };
        var services = CreateProvider(services =>
        {
            services.AddSingleton<TestService>();

            services.AddAggregate<JsonObject, JsonObject>(b => b
                .ExecuteInParallel()
                .AddServiceCall<TestService>(
                    name: "test",
                    call: (service, request, ct) => throw new Exception("failed"),
                    fallback: (sp, request, ex) => new(result: null)
                )
            );
        });

        var executor = services.GetRequiredService<AggregateExecutor<JsonObject, JsonObject>>();
        var r1 = executor.Execute(request, default);

        await Assert.That(r1).IsTypeOf<Result<JsonObject>.Er>();
    }
}

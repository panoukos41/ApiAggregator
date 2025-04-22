using Microsoft.Extensions.DependencyInjection;

namespace ApiAggregator.Test.Unit;

public abstract class TestBase
{
    protected static IServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);

        services.AddHybridCache();
        services.AddLogging();

        return services.BuildServiceProvider();
    }
}

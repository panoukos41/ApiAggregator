using Flurl.Http;
using Microsoft.Extensions.Options;

namespace ApiAggregator.Clients;

public abstract class ClientBase<TOptions> where TOptions : class
{
    protected TOptions Options { get; }

    protected FlurlClient Client { get; }

    protected ClientBase(HttpClient httpClient, IOptionsSnapshot<TOptions> options)
    {
        Client = new(httpClient);
        Options = options.Value;
    }
}

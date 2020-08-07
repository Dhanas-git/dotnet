using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IRouterClient
    {
        HttpClient Client { get; }
    }
}
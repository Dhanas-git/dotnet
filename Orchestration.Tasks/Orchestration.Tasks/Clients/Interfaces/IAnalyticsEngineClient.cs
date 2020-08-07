using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IAnalyticsEngineClient
    {
        HttpClient Client { get; }
    }
}
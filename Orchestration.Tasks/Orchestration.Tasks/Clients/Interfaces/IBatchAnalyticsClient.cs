using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IBatchAnalyticsClient
    {
        HttpClient Client { get; }
    }
}
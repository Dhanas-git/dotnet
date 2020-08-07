using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface ITAOrchestratorClient
    {
        HttpClient Client { get; }
    }
}
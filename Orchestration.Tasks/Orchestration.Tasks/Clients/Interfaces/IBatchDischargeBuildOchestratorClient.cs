using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IBatchDischargeBuildOrchestratorClient
    {
        HttpClient Client { get; }
    }
}
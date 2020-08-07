using System.Net.Http;

namespace Orchestration.Tasks
{
    public interface IBatchEventBuildOchestratorClient
    {
        HttpClient Client { get; }
    }
}
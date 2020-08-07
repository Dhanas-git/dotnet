using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class BatchEventBuildOrchestratorClient : IBatchEventBuildOchestratorClient
    {
        public HttpClient Client { get; }

        public BatchEventBuildOrchestratorClient(HttpClient client)
        {
            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.BatchEventBuildOrchestrationUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientBatchAnalyticsTimeout);
            Client = client;
        }
    }
}
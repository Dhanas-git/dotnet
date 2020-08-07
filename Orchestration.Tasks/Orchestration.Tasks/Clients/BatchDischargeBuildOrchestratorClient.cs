using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class BatchDischargeBuildOrchestratorClient:IBatchDischargeBuildOrchestratorClient
    {
        public HttpClient Client { get; }

        public BatchDischargeBuildOrchestratorClient(HttpClient client)
        {
            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.DischargeSinkStatusUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientBatchAnalyticsTimeout);
            Client = client;
        }
    }
}
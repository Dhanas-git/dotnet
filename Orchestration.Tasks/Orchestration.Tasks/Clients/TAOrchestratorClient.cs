using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class TAOrchestratorClient : ITAOrchestratorClient
    {
        public HttpClient Client { get; }

        public TAOrchestratorClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.TAOrchestrationUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientBatchAnalyticsTimeout);
            Client = client;

        }

    }
}

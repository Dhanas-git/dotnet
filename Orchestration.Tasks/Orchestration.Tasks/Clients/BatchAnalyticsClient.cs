using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class BatchAnalyticsClient : IBatchAnalyticsClient
    {
        public HttpClient Client { get; }

        public BatchAnalyticsClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.BatchAnalyticsUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientBatchAnalyticsTimeout);
            Client = client;

        }

    }
}

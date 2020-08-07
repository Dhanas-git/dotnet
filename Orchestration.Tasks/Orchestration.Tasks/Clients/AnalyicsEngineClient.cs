using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class AnalyticsEngineClient : IAnalyticsEngineClient
    {
        public HttpClient Client { get; }

        public AnalyticsEngineClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.AnalyticsEngineUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientAnalyticsEngineTimeout);
            Client = client;

        }

    }
}

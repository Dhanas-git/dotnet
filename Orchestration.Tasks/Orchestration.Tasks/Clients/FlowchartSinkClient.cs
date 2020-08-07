using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class FlowchartSinkClient : IFlowchartSinkClient
    {
        public HttpClient Client { get; }

        public FlowchartSinkClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.FlowchartSinkStatusUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientFlowchartSinkTimeout);
            Client = client;

        }

    }
}

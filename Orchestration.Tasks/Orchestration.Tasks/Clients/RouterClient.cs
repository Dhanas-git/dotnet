using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class RouterClient : IRouterClient
    {
        public HttpClient Client { get; }

        public RouterClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.RouterUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientRouterTimeout);
            Client = client;

        }

    }
}

using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class JsonManagerClient : IJsonManagerClient
    {
        public HttpClient Client { get; }

        public JsonManagerClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.JsonManagerUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientJsonManagerTimeout);
            Client = client;

        }

    }
}

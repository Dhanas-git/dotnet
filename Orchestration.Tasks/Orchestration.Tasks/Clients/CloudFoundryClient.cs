using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class CloudFoundryClient : ICloudFoundryClient
    {
        public HttpClient Client { get; }

        public CloudFoundryClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.CfClientUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientCloudFoundryTimeout);
            Client = client;

        }

    }
}

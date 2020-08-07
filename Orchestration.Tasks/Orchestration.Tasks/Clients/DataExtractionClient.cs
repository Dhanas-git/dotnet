using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class DataExtractionClient : IDataExtractionClient
    {
        public HttpClient Client { get; }

        public DataExtractionClient(HttpClient client)
        {

            var appConfig = new AppConfig();
            client.BaseAddress = new Uri(appConfig.DataExtractionUri);
            client.Timeout = new TimeSpan(appConfig.ClientDataExtractionTimeout, 0, 0);
            Client = client;

        }
    }
}

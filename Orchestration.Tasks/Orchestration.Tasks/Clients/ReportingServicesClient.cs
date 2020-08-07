using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class ReportingServicesClient : IReportingServicesClient
    {
        public HttpClient Client { get; }

        public ReportingServicesClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.ReportingServicesUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientReportingServicesTimeout);
            Client = client;

        }

    }
}

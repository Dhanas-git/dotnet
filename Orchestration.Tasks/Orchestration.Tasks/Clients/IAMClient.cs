using Orchestration.Shared.Domain.IAM;
using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class IAMClient : IIAMClient
    {
        public HttpClient Client { get; }

        public IAMClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.IAMCustomerManagementUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientIAMCustomerManagementTimeout);
            Client = client;

        }

    }
}

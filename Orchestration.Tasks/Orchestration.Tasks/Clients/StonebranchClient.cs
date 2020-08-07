using Orchestration.Shared.Orchestrator;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Orchestration.Tasks.Clients
{
    public class StonebranchClient : IStonebranchClient
    {

        public HttpClient Client { get; }

        public StonebranchClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            var stonebranchUser = $"{appConfig.StonebranchUser}:{appConfig.StonebranchPassword}";
            var stonebranchAuth = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(stonebranchUser)));

            client.BaseAddress = new Uri(appConfig.StonebranchUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientStonebranchTimeout);
            client.DefaultRequestHeaders.Authorization = stonebranchAuth;

            Client = client;

        }

    }
}

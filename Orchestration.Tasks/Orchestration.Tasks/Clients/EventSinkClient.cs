using System;
using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public class EventSinkClient : IEventSinkClient
    {
        public HttpClient Client { get; }

        public EventSinkClient(HttpClient client)
        {

            var appConfig = new AppConfig();

            client.BaseAddress = new Uri(appConfig.EventSinkStatusUri);
            client.Timeout = new TimeSpan(0, 0, appConfig.ClientEventSinkTimeout);
            Client = client;

        }

    }
}

using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IEventSinkClient
    {
        HttpClient Client { get; }
    }
}
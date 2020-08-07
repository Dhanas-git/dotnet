using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IJsonManagerClient
    {
        HttpClient Client { get; }
    }
}
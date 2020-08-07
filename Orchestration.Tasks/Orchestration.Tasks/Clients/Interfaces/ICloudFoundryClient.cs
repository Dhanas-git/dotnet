using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface ICloudFoundryClient
    {
        HttpClient Client { get; }
    }
}
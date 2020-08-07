using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IDataExtractionClient
    {
        HttpClient Client { get; }
    }
}

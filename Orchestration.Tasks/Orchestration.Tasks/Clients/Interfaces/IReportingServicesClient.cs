using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IReportingServicesClient
    {
        HttpClient Client { get; }
    }
}
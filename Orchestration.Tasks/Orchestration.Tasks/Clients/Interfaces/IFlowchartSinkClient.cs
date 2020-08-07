using System.Net.Http;

namespace Orchestration.Tasks.Clients
{
    public interface IFlowchartSinkClient
    {
        HttpClient Client { get; }
    }
}
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Orchestration.Backbone.Domain;
using Orchestration.Shared;
using Orchestration.Tasks.Clients;

namespace Orchestration.Tasks.Models
{
    public class AnalyticsRunSummary : IAnalyticsRunSummary
    {

        private HttpClient _httpClient;
        private IValidation _validation;

        public AnalyticsRunSummary(ITAOrchestratorClient client, IValidation validation)
        {
            _httpClient = client.Client;
            _validation = validation;
        }

        public AnalyticsRunSummary(IEventSinkClient eventSinkClient, IValidation validation)
        {
            _httpClient = eventSinkClient.Client;
            _validation = validation;
        }

        public AnalyticsRunSummary(IFlowchartSinkClient flowchartSinkClient, IValidation validation)
        {
            _httpClient = flowchartSinkClient.Client;
            _validation = validation;
        }

        public AnalyticsSummary GetRunSummary(string customerShortName, string projectShortName, Guid id)
        {

            var input = new
            {
                client = customerShortName,
                project = projectShortName,
                analyticsRunUUID = id
            };

            var json = JsonConvert.SerializeObject(input);
            AppConfig appConfig = new AppConfig();

            var response = _httpClient.PostAsync(appConfig.SummaryAPIMethod, new StringContent(json, Encoding.UTF8, "application/json")).Result;
            _validation.ValidateResponse(response);

            var result = response.Content.ReadAsStringAsync().Result;
            var analyticsSummary = JsonConvert.DeserializeObject<AnalyticsSummary>(result);

            return analyticsSummary;

        }
    }
}

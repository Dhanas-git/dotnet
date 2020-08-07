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
    public class AnalyticsRunStatusProxy : IAnalyticsRunStatusProxy
    {

        private HttpClient _httpClient; 
        private IValidation _validation;
        private ILogging _logging;

        public AnalyticsRunStatusProxy(ITAOrchestratorClient client, IValidation validation, ILogging logging)
        {
            _httpClient = client.Client;
            _validation = validation;
            _logging = logging;
        }

        public AnalyticsRunStatusProxy(IEventSinkClient eventSinkClient, IValidation validation, ILogging logging)
        {
            _httpClient = eventSinkClient.Client;
            _validation = validation;
            _logging = logging;
        }


        public AnalyticsRunStatusProxy(IBatchDischargeBuildOrchestratorClient dischargeSinkClient, IValidation validation, ILogging logging)
        {
            _httpClient = dischargeSinkClient.Client;
            _validation = validation;
            _logging = logging;
        }


        public AnalyticsRunStatusProxy(IFlowchartSinkClient flowchartSinkClient, IValidation validation, ILogging logging)
        {
            _httpClient = flowchartSinkClient.Client;
            _validation = validation;
            _logging = logging;
        }

        public AnalyticsRunStatusProxy(IBatchEventBuildOchestratorClient batchEventBuildOchestratorClient, IValidation validation, ILogging logging)
        {
            _httpClient = batchEventBuildOchestratorClient.Client;
            _validation = validation;   
            _logging = logging;
        }


        public AnalyticsStatus GetRunStatus(string customerShortName, string projectShortName, Guid id)
        {

            var input = new
            {
                client = customerShortName,
                project = projectShortName,
                analyticsRunUUID = id
            };

            var json = JsonConvert.SerializeObject(input);
            AppConfig appConfig = new AppConfig();
            var response = _httpClient.PostAsync(appConfig.StatusAPIMethod, new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);

            var result = response.Content.ReadAsStringAsync().Result;

            try
            {
                var analyticsStatus = JsonConvert.DeserializeObject<AnalyticsStatus>(result);
                return analyticsStatus;
            }
            catch
            {
                _logging.Log("Failed to deserialize, received this message: " + result, Shared.Domain.Log.LogLevels.Error);
                throw;
            }
        }
    }
}

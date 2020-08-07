#region Copyright © 2018 Inovalon

//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Orchestration.Backbone.Domain;
using Orchestration.Shared;
using Orchestration.Shared.AnalyticsEngine;
using Orchestration.Tasks.Clients;

namespace Orchestration.Tasks.Models
{
    public class WorkStatusProxy : IWorkStatusProxy
    {

        #region Private Properties

        private IAnalyticsEngineClient _analyticsEngineClient;
        private IValidation _validation;

        #endregion

        #region Public Constructors

        public WorkStatusProxy(IAnalyticsEngineClient analyticsEngineClient, IValidation validation)
        {
            _analyticsEngineClient = analyticsEngineClient;
            _validation = validation;
        }

        #endregion

        #region Public Methods

        public void Add(IOrchestrationJob job, AnalyticsRunStatus taskStatus, int progress, bool writeChildren, bool canCancel)
        {

            var statuses = new List<WorkStatus>();
            statuses.Add(new WorkStatus(job.analyticsRunUUID, canCancel, progress, taskStatus));

            if (writeChildren)
            {
                job.flowchartRunRequest.ToList().ForEach(x =>
                {
                    statuses.Add(new WorkStatus(x.flowchartRunUUID, progress, taskStatus));
                });
            }

            Add(statuses);

        }

        public void Add(IOrchestrationJob job, AnalyticsRunStatus taskStatus, List<WorkStatusMessage> messages, int progress, bool writeChildren, bool canCancel)
        {

            var statuses = new List<WorkStatus>();

            var status = new WorkStatus(job.analyticsRunUUID, canCancel, progress, taskStatus);
            status.LoggableMessages = messages;

            statuses.Add(status);

            if (writeChildren)
            {
                job.flowchartRunRequest.ToList().ForEach(x =>
                {

                    var childStatus = new WorkStatus(x.flowchartRunUUID, progress, taskStatus);
                    childStatus.LoggableMessages = messages;

                    statuses.Add(childStatus);

                });
            }

            Add(statuses);

        }

        public void Add(WorkStatus workStatus)
        {
            Add(new List<WorkStatus>() { workStatus });
        }

        public void Add(List<WorkStatus> statuses)
        {

            var json = JsonConvert.SerializeObject(statuses);
            var response = _analyticsEngineClient.Client.PostAsync("AddWorkStatus", new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);

        }

        public WorkStatus GetLastStatus(string customerShortName, string projectShortName, Guid workId, ViewLevel viewLevel = ViewLevel.All, int maxMessages = 10)
        {

            var method = $"getWorkStatus?customerShortName={customerShortName}&projectShortName={projectShortName}&workId={workId}&viewLevel={viewLevel}&max_messages={maxMessages}";
            var response = _analyticsEngineClient.Client.GetAsync(method).Result;

            _validation.ValidateResponse(response);
            return JsonConvert.DeserializeObject<WorkStatus>(response.Content.ReadAsStringAsync().Result);

        }

        public Dictionary<string, string> GetWorkProperties(string customerShortName, Guid workId)
        {

            var method = $"getWorkProperties?customerShortName={customerShortName}&jobId={workId}";
            var response = _analyticsEngineClient.Client.GetAsync(method).Result;

            _validation.ValidateResponse(response);
            return new Dictionary<string, string>(JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(response.Content.ReadAsStringAsync().Result));

        }

        public string GetWorkProperty(string customerShortName, Guid workId, string property)
        {

            var properties = GetWorkProperties(customerShortName, workId);
            return properties.GetValueOrDefault(property);

        }

        #endregion

    }
}

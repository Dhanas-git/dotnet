#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Orchestration.Data;
using Orchestration.Data.Models;
using Orchestration.Shared;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Orchestration.Tasks.Controllers
{

    /// <summary>
    /// Provides rest apis for publishing metadata.
    /// </summary>    
    public class MetadataController : Controller
    {

        #region Private Properties

        private IAppConfig _config;

        private IJobProxy _jobProxy;
        private ILogging _logging;
        private ITaskLogging _taskLogging;
        private IValidation _validation;
        private IWorkStatusProxy _workStatusProxy;

        #endregion

        #region Public Constructors	

        public MetadataController(IAppConfig config,
                                  IJobProxy jobProxy,
                                  ILogging logging,
                                  ITaskLogging taskLogging,
                                  IValidation validation,
                                  IWorkStatusProxy workStatusProxy)
        {

            _config = config;

            _jobProxy = jobProxy;
            _logging = logging;
            _taskLogging = taskLogging;
            _validation = validation;
            _workStatusProxy = workStatusProxy;

        }

        #endregion

        #region Private Methods



        #endregion

        #region Public Methods       

        /// <summary>
        /// Inserts or updates analytics run metadata.
        /// </summary>		
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="job">analytics run id</param>
        /// <param name="store">metadata store</param>
        [HttpPut]
        public IActionResult PublishGreenplum([FromServices] IReportingServicesClient reportingServicesClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, 0, true, true);

            var content = job.flowchartRunRequest
                                .SelectMany(x => (x.flowchartCatalogMetadata), (request, metadata) => new { flowchartRunId = request.flowchartRunUUID, catalogId = metadata.flowchartCatalogID })
                                .GroupBy(x => x.catalogId)
                                .ToDictionary(g => g.Key, g => g.Select(x => x.flowchartRunId).ToList());

            var i = 0;
            var length = content.Keys.Count;

            content.Keys.ToList().ForEach(x =>
            {

                var input = new
                {
                    customerShortName = customerShortName,
                    projectShortName = projectShortName,
                    flowchartContentItemId = x,
                    vaccuum = (i == (length - 1)) ? true : false
                };

                var json = JsonConvert.SerializeObject(input);
                var method = "publishflowchartMetaDataGreenPlum_Rest";
                var stopWatch = Stopwatch.StartNew();

                var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                _validation.ValidateResponse(response);
                _taskLogging.LogOperation(customerShortName, projectShortName, "PublishMetadataGreenplum", job, stopWatch.Elapsed);

                var result = response.Content.ReadAsStringAsync().Result;
                var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

                if (!succeeded)
                {
                    throw new OperationCanceledException($"Published failed for the following input: api: {method}, json: {json}");
                }

                i++;

            });

            return Ok();

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="reportingServicesClient"></param>
        /// <param name="customerShortName"></param>
        /// <param name="projectShortName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut]
        public IActionResult PublishEventMetaDataGreenplum([FromServices] IReportingServicesClient reportingServicesClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, 0, true, true);

            var eventCatalogs = job.flowchartRunRequest.SelectMany(fcr => fcr.flowchartCatalogMetadata.SelectMany(fcm => fcm.eventCatalogs));

            if (eventCatalogs.Count() > 0)
            {
                var input = new
                {
                    customerShortName = customerShortName,
                    projectShortName = projectShortName,
                    eventCatalogs = eventCatalogs
                };

                var json = JsonConvert.SerializeObject(input);
                var method = "PublishEventCatalogMetadataToGreenplum_Rest";
                var stopWatch = Stopwatch.StartNew();

                var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                _validation.ValidateResponse(response);
                _taskLogging.LogOperation(customerShortName, projectShortName, "PublishEventCatalogMetadataToGreenplum_Rest", job, stopWatch.Elapsed);

                var result = response.Content.ReadAsStringAsync().Result;
                var failedEventCatalogs = ((JProperty)JObject.Parse(result).First).Value;

                if (failedEventCatalogs.Count() > 0)
                {
                    throw new OperationCanceledException($"Event Catalog sync failed for the following input: api: {method}, error:{failedEventCatalogs} json: {json}");
                }
            }

            return Ok();

        }

        /// <summary>
        /// Inserts or updates analytics run metadata.
        /// </summary>		
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="job">analytics run id</param>
        /// <param name="store">metadata store</param>
        [HttpPut]
        public IActionResult PublishSql([FromServices] IReportingServicesClient reportingServicesClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, 0, true, true);

            var content = job.flowchartRunRequest
                                .SelectMany(x => (x.flowchartCatalogMetadata), (request, metadata) => new { flowchartRunId = request.flowchartRunUUID, catalogId = metadata.flowchartCatalogID })
                                .GroupBy(x => x.catalogId)
                                .ToDictionary(g => g.Key, g => g.Select(x => x.flowchartRunId).ToList());

            content.Keys.ToList().ForEach(x =>
            {

                var input = new
                {
                    customerShortName = customerShortName,
                    projectShortName = projectShortName,
                    flowchartContentItemId = x,
                    flowchartRunIds = content[x]
                };

                var json = JsonConvert.SerializeObject(input);
                var method = "publishFlowchartMetadataReportingDB_Rest";
                var stopwatch = Stopwatch.StartNew();

                var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                _validation.ValidateResponse(response);
                _taskLogging.LogOperation(customerShortName, projectShortName, "PublishMetadataSql", job, stopwatch.Elapsed);

                var result = response.Content.ReadAsStringAsync().Result;
                var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

                if (!succeeded)
                {
                    throw new OperationCanceledException($"Published failed for the following input: api: {method}, json: {json}");
                }

            });

            return Ok();

        }

        [HttpGet]
        public IActionResult TestLongRunning([FromServices] IDataClient dataClient, [FromServices] IIAM iam, string customerShortName, string projectShortName, int timeout)
        {

            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_timeout", NpgsqlDbType.Integer) { Value = timeout },
                };

            var projectConfig = iam.GetProjectConfig(customerShortName, projectShortName);
            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_timeout_test", parameters);

            var stopWatch = Stopwatch.StartNew();
            dataClient.ExecuteScalar<object>(request);

            _logging.Log($"Long-running-test ran for: { stopWatch.Elapsed }");
            return Ok();

        }

        #endregion

    }
}

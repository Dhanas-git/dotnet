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
using Orchestration.Shared.Orchestrator;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Orchestration.Tasks.Controllers
{

    public class SetupRunController : Controller
    {

        #region Private Properties

        private IAppConfig _config;
        private IIAM _iam;
        private IJobProxy _jobProxy;
        private ILogging _logging;
        private ITaskLogging _taskLogging;
        private IValidation _validation;
        private IWorkStatusProxy _workStatusProxy;

        #endregion

        #region Public Constructors	        

        public SetupRunController(IAppConfig config,
                                   IJobProxy jobProxy,
                                   IIAM iam,
                                   ILogging logging,
                                   ITaskLogging taskLogging,
                                   IValidation validation,
                                   IWorkStatusProxy workStatusProxy)
        {

            _config = config;

            _iam = iam;
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
        /// Logs params to big data repository for each catalog in each flowchart run and updates Analytics 
        /// Engine DB with the status. 
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public ActionResult LogParams([FromServices] IReportingServicesClient reportingServicesClient, string customerShortName, string projectShortName, Guid id)
        {

            var listWorkStatus = new List<WorkStatus>();
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, Calculations.Percentage(0, 13, 14, _config.PreTAPercentageContribution), true, true);

            // Add status to parent work id with WorkStatus of 'Running'
            listWorkStatus.Add(new WorkStatus(job.analyticsRunUUID, true, _config.PreTAPercentageContribution, AnalyticsRunStatus.Running));
            var stopWatch = Stopwatch.StartNew();

            job.flowchartRunRequest.ToList().ForEach(flowchartRunRequest =>
            {
                flowchartRunRequest.flowchartCatalogMetadata.ToList().ForEach(
                    catalog =>
                    {
                        var advancedOptions = new string[16, 2]
                        {
                            {"continuousEnrollmentVariable", flowchartRunRequest.continuousEnrollmentVariable},
                            {"engine_type", "Orchestration" },
                            {"runType", flowchartRunRequest.runType},
                            {"excludeHospice", flowchartRunRequest.excludeHospice.ToString()},
                            {"hedisPPO", flowchartRunRequest.hedisPPO.ToString()},
                            {"ignoreOneDay", flowchartRunRequest.ignoreOneDay.ToString()},
                            {"includeDetailResults", flowchartRunRequest.includeDetailResults.ToString()},
                            {"includeFlaggedEventResults", flowchartRunRequest.includeFlaggedEventResults.ToString()},
                            {"includeGlobalEvents", flowchartRunRequest.includeGlobalEvents.ToString()},
                            {"includeMemberMonthResults", flowchartRunRequest.includeMemberMonthResults.ToString()},
                            {"includeMessageResults", flowchartRunRequest.includeMessageResults.ToString()},
                            {"nonDenomDetail", flowchartRunRequest.nonDenomDetail.ToString()},
                            {"reportingYearBeginDate", flowchartRunRequest.reportingYearBeginDate.ToString(CultureInfo.InvariantCulture)},
                            {"reportingYearEndDate", flowchartRunRequest.reportingYearEndDate.ToString(CultureInfo.InvariantCulture)},
                            {"skipContinuousEnrollment", flowchartRunRequest.skipContinuousEnrollment.ToString()},
                            {"skipHasBenefit", flowchartRunRequest.skipHasBenefit.ToString()},
                        };

                        var input = new
                        {
                            customerShortName = customerShortName,
                            projectShortName = projectShortName,
                            resultSchemaName = projectConfig.GreenplumConfig.ResultSchema,
                            flowchartRunGuid = flowchartRunRequest.flowchartRunUUID,
                            runId = catalog.flowchartRunID,
                            populationIds = job.populationIds ?? new List<int>(),
                            advancedOptions = advancedOptions,
                            eventCatalogs = catalog.eventCatalogs.Select(x => new string[] { x.eventCatalogID.ToString(), x.name }).ToArray(),
                            promptVariables = catalog.promptVariables.Select(x => new string[] { x.variableName, x.value }).ToArray(),
                            gpConnectionString = projectConfig.GreenplumConfig.RawConnectionString,
                            sampleOptionsXml = string.Empty
                        };


                        var json = JsonConvert.SerializeObject(input);
                        var method = "LogParametersToGreenplum_REST";

                        var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;
                        _validation.ValidateResponse(response);

                        var result = response.Content.ReadAsStringAsync().Result;
                        var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

                        if (!succeeded)
                        {
                            throw new OperationCanceledException($"Logging failed for the following input: method: {method}, json: {json}");
                        }


                    });

                // Add status to child flowchart work id with WorkStatus of 'Running'
                listWorkStatus.Add(new WorkStatus(flowchartRunRequest.flowchartRunUUID, _config.PreTAPercentageContribution, AnalyticsRunStatus.Running));

            });

            _workStatusProxy.Add(listWorkStatus);
            _taskLogging.LogOperation(customerShortName, projectShortName, "LogParams", job, stopWatch.Elapsed);

            return Ok();

        }

        /// <summary>
        /// Calls manage output combined for each flowchart run.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public ActionResult ManageOutputCombined([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, Calculations.Percentage(0, 12, 14, _config.PreTAPercentageContribution), true, true);

            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);
            var stopWatch = Stopwatch.StartNew();

            job.flowchartRunRequest.ToList().ForEach(flowchartRunRequest =>
            {

                var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_result_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema },
                    new NpgsqlParameter("v_flowchart_run_uuid", NpgsqlDbType.Varchar) { Value = flowchartRunRequest.flowchartRunUUID.ToString() }
                };

                var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_manage_combined_output_tables", parameters);
                dataClient.ExecuteScalar<object>(request);

            });

            _taskLogging.LogOperation(customerShortName, projectShortName, "ManageOutputCombined", job, stopWatch.Elapsed);
            return Ok();

        }

        /// <summary>
        /// Refreshes detail expanded view table.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public ActionResult RefreshDetailExpandedView([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            _workStatusProxy.Add(new WorkStatus(id, true, Calculations.Percentage(0, 9, 14, _config.PreTAPercentageContribution), AnalyticsRunStatus.Running));
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_source_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.SourceSchema },
                    new NpgsqlParameter("v_result_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema }
                };

            var stopWatch = Stopwatch.StartNew();

            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_detail_expanded_view", parameters);
            dataClient.ExecuteScalar<object>(request);

            _taskLogging.LogOperation(customerShortName, projectShortName, "RefreshDetailExpandedView", job, stopWatch.Elapsed);
            return Ok();

        }

        /// <summary>
        /// Refreshes enrollment combined table.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public ActionResult RefreshEnrollmentCombined([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            _workStatusProxy.Add(new WorkStatus(id, true, Calculations.Percentage(0, 7, 14, _config.PreTAPercentageContribution), AnalyticsRunStatus.Running));
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_source_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.SourceSchema },
                    new NpgsqlParameter("v_result_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema }
                };

            var stopWatch = Stopwatch.StartNew();

            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_enrollment_combined", parameters);
            dataClient.ExecuteScalar<object>(request);

            _taskLogging.LogOperation(customerShortName, projectShortName, "RefreshEnrollmentCombined", job, stopWatch.Elapsed);
            return Ok();

        }

        /// <summary>
        /// Refreshes member combined table.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public ActionResult RefreshMemberCombined([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            _workStatusProxy.Add(new WorkStatus(id, true, Calculations.Percentage(0, 5, 14, _config.PreTAPercentageContribution), AnalyticsRunStatus.Running));
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_source_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.SourceSchema },
                    new NpgsqlParameter("v_result_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema }
                };

            var stopWatch = Stopwatch.StartNew();

            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_member_combined", parameters);
            dataClient.ExecuteScalar<object>(request);

            _taskLogging.LogOperation(customerShortName, projectShortName, "RefreshMemberCombined", job, stopWatch.Elapsed);
            return Ok();

        }

        /// <summary>
        /// Uses the router to start refreshing populations.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPost]
        public ActionResult PostRefreshPopulations([FromServices] IRouterClient routerClient, [FromServices] IStonebranchClient stonebranchClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            var requestId = Guid.NewGuid();

            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, Calculations.Percentage(0, 3, 14, _config.PreTAPercentageContribution), true, true);

            var populationIds = (job.populationIds == null || job.populationIds.Count < 1) ? string.Empty : String.Join(',', job.populationIds);
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);
            var stopWatch = Stopwatch.StartNew();

            var json = $@"
                            {{
                              'requestUUID': '{requestId}',
                              'customerShortName': '{customerShortName}',
                              'projectShortName': '{projectShortName}',
                              'requestType': 'ReprocessPopulation',
                              'requestData': {{
                                    'populationIds': [{populationIds}],
                                    'forceRefresh': false,
                               }}
                            }}
                         ";

            var response = routerClient.Client.PostAsync("postRequest", new StringContent(json, Encoding.UTF8, "application/json")).Result;
            _validation.ValidateResponse(response);

            if (job.lastRouterRequests.Keys.Contains("RefreshPopulations"))
            {
                job.lastRouterRequests["RefreshPopulations"] = requestId.ToString();
            }
            else
            {
                job.lastRouterRequests.Add("RefreshPopulations", requestId.ToString());
            }

            _jobProxy.UpdateJob(job);

            _taskLogging.LogOperation(customerShortName, projectShortName, "Refresh Populations message posted.", job, stopWatch.Elapsed);
            return Ok(requestId);

        }

        /// <summary>
        /// Uses the router to poll a refresh populations request.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpGet]
        public IActionResult IsRefreshPopulationsFinished([FromServices] IRouterClient routerClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            if (!job.lastRouterRequests.Keys.Contains("RefreshPopulations"))
            {
                throw new InvalidOperationException("Could not find any router requests for: RefreshPopulations");
            }

            var stopWatch = Stopwatch.StartNew();

            var response = routerClient.Client.GetAsync($"getRequestStatus?requestUuid={job.lastRouterRequests["RefreshPopulations"]}").Result;
            _validation.ValidateResponse(response);

            _taskLogging.LogOperation(customerShortName, projectShortName, "PollRefreshPopulations", job, stopWatch.Elapsed);

            var content = response.Content.ReadAsStringAsync();
            return Ok(content.Result);

        }

        /// <summary>
        /// Calls upsert flowchart run for every catalog for each flowchart run.
        /// </summary>
        /// <param name="customerShortName">cusomter short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <returns>ok</returns>
        [HttpPut]
        public IActionResult UpsertFlowchartRun([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, Calculations.Percentage(0, 11, 14, _config.PreTAPercentageContribution), true, true);
            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var stopWatch = Stopwatch.StartNew();

            job.flowchartRunRequest.ToList().ForEach(flowchartRunRequest => flowchartRunRequest.flowchartCatalogMetadata.ToList().ForEach(
                catalog =>
                {

                    var beginDate = DateTime.Parse(flowchartRunRequest.reportingYearBeginDate);
                    var endDate = DateTime.Parse(flowchartRunRequest.reportingYearEndDate);

                    var parameters = new List<NpgsqlParameter>
                    {
                        new NpgsqlParameter("result_schema_name", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema },
                        new NpgsqlParameter("fc_catalog_id", DbType.Int64) { Value = catalog.flowchartCatalogID },
                        new NpgsqlParameter("flowchartrunuuid", NpgsqlDbType.Varchar) { Value = flowchartRunRequest.flowchartRunUUID.ToString() },
                        new NpgsqlParameter("fc_run_name", NpgsqlDbType.Varchar) { Value = flowchartRunRequest.name},
                        new NpgsqlParameter("fc_run_date_time", NpgsqlDbType.Varchar) { Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                        new NpgsqlParameter("fc_begin_date", NpgsqlDbType.Varchar) { Value = beginDate.ToString("yyyy-MM-dd") },
                        new NpgsqlParameter("fc_end_date", NpgsqlDbType.Varchar) { Value = endDate.ToString("yyyy-MM-dd") },
                        new NpgsqlParameter("fc_catalog_name", NpgsqlDbType.Varchar) { Value = catalog.flowchartCatalogKey }
                    };

                    var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_insert_update_flowchartrun", parameters);
                    catalog.flowchartRunID = dataClient.ExecuteScalar<long>(request);

                }));

            _jobProxy.UpdateJob(job);
            _taskLogging.LogOperation(customerShortName, projectShortName, "UpsertFlowchartRun", job, stopWatch.Elapsed);

            return Ok();

        }


        /// <summary>
        /// Uses the router to start refreshing populations.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <param name="flowchartRunId">Run Id</param>
        /// <returns>ok</returns>
        [HttpPost]
        public ActionResult PostUpdateSampleStatus([FromServices] IRouterClient routerClient, string customerShortName, string projectShortName, Guid id, Guid flowchartRunId)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            var flowchartRun = job.flowchartRunRequest.FirstOrDefault(x => x.flowchartRunUUID == flowchartRunId);
            if (flowchartRun == null) { throw new NullReferenceException("The flowchart run was not present in the job."); }

            List<Guid> requestIds = new List<Guid>();
            var updateSampleKey = $"UpdateSampleStatus_{flowchartRunId.ToString()}";
            var stopWatch = Stopwatch.StartNew();

            foreach (var sample in flowchartRun.hrSampleMeasureMetadata)
            {
                var requestId = Guid.NewGuid();

                var json = $@"
                            {{
                              'requestUUID': '{requestId}',
                              'customerShortName': '{customerShortName}',
                              'projectShortName': '{projectShortName}',
                              'requestType': 'Sampling',
                              'requestData': 
                              {{
                                    'samplingData': 
                                    {{
                                        'samplingRequestType': 'UpdateSampleStatus',
                                        'samplingRequest': 
                                        {{
                                            'measureKey': '',
                                            'runGenerateHybridIdssReport':'false',
                                            'flowchartRunGUID': '{flowchartRun.flowchartRunUUID}',
                                            'hrSampleMeasureId': '{sample.Key}',
                                            'samplingOptions': 
                                                {{
                                                    'activateOverSample': '{sample.Value.activateOverSample}',
                                                    'leaveNumeratorHitOpen': '{sample.Value.leaveNumeratorHitOpen}',
                                                    'removeContrasBeforeSample': '{sample.Value.removeContrasBeforeSample}',
                                                    'excludeContrasNextRun': '{sample.Value.excludeContrasNextRun}',
                                                    'excludePlanEmployees': '{sample.Value.excludePlanEmployees}',
                                                    'excludePopulationFallout': '{sample.Value.excludePopulationFallout}',
                                                    'metadataReportingYear': '{sample.Value.metadataReportingYear}'
                                                }}
                                        }}
                                    }}
                               }}
                            }}";


                var response = routerClient.Client.PostAsync("postRequest", new StringContent(json, Encoding.UTF8, "application/json")).Result;
                _validation.ValidateResponse(response);

                requestIds.Add(requestId);


            }
            // Go ahead and add the empty list to the so that we can skip the next task
            if (job.lastRouterRequests.Keys.Contains(updateSampleKey))
            {
                job.lastRouterRequests[updateSampleKey] = JsonConvert.SerializeObject(requestIds);
            }
            else
            {
                job.lastRouterRequests.Add(updateSampleKey, JsonConvert.SerializeObject(requestIds));
            }
            _jobProxy.UpdateJob(job);
            if (requestIds.Count > 0)
            {
                _taskLogging.LogOperation(customerShortName, projectShortName, "UpdateSampleStatus message posted.", flowchartRun.flowchartRunUUID, stopWatch.Elapsed, _config.CacheDBPercentageContribution);
            }
            else
            {
                _taskLogging.LogOperation(customerShortName, projectShortName, "Skipping Update Sample Status as there is not sample data.", flowchartRun.flowchartRunUUID, stopWatch.Elapsed, _config.CacheDBPercentageContribution);
            }

            return Ok();

        }

        /// <summary>
        /// Uses the router to poll a refresh populations request.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <param name="flowchartRunId">Run id</param>
        /// <returns>ok</returns>
        [HttpGet]
        public IActionResult IsUpdateSampleStatusFinished([FromServices] IRouterClient routerClient, string customerShortName, string projectShortName, Guid id, Guid flowchartRunId)
        {
            var key = $"UpdateSampleStatus_{flowchartRunId.ToString()}";
            try
            {
                var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

                List<Guid> pendingRequests = new List<Guid>();
                List<Exception> failures = new List<Exception>();

                if (!job.lastRouterRequests.Keys.Contains(key))
                {
                    throw new InvalidOperationException($"Could not find any router requests for: UpdateSampleStatus_{ flowchartRunId.ToString() }");
                }

                var requestIds = JsonConvert.DeserializeObject<List<Guid>>(job.lastRouterRequests[key]);

                if (requestIds.Count > 0)
                {
                    pendingRequests = new List<Guid>(requestIds);

                    while (pendingRequests.Count > 0)
                    {
                        // Retry only pending requests
                        requestIds.Intersect(pendingRequests).ToList().ForEach((request) =>
                        {
                            var response = routerClient.Client.GetAsync($"getRequestStatus?requestUuid={request}").Result;
                            _validation.ValidateResponse(response);
                            var content = response.Content.ReadAsStringAsync();
                            var json = JObject.Parse(content.Result);
                            var status = json["status"].Value<int>();
                            switch (status)
                            {
                                case 5:
                                    pendingRequests.Remove(request);
                                    break;
                                case 7:
                                    pendingRequests.Remove(request);
                                    failures.Add(new Exception($"Update sample status request('{request}') failed: {json["exception_message"].Value<string>()} \n"));
                                    break;
                                default:
                                    break;
                            }
                        });
                    }

                }

                if (failures.Count > 0)
                {
                    throw new AggregateException(failures);
                }
            }
            catch (JsonReaderException)
            {
                throw new JsonReaderException($"Invalid json string for key {key} ");
            }
            catch (Exception)
            {

                throw;
            }

            return Ok();

        }
        #endregion

    }
}
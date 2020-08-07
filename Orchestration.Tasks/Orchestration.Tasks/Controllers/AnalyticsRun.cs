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
using Orchestration.Backbone.Domain;
using Orchestration.Data;
using Orchestration.Data.Models;
using Orchestration.Shared;
using Orchestration.Shared.AnalyticsEngine;
using Orchestration.Shared.CloudFoundry;
using Orchestration.Shared.Orchestrator;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestration.Tasks.Controllers
{

    public class AnalyticsRunController : Controller
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

        public AnalyticsRunController(IAppConfig config,
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

        #region Public Enumerations

        public enum WorkflowType
        {
            AnalyticsRun,
            CacheDB,
            Unknown
        }

        public enum ApplicationType
        {
            TA,
            EventSink,
            FlowchartSink,
            BatchEventBuild,
            DischargeSink
        }

        #endregion

        #region Private Methods

        private WorkflowType ParseWorkflow(string workflowName)
        {

            if (workflowName.Contains("CacheDB"))
            {
                return WorkflowType.CacheDB;
            }
            else if (workflowName.Contains("Analytics_Run"))
            {
                return WorkflowType.AnalyticsRun;
            }
            else
            {
                return WorkflowType.Unknown;
            }

        }

        private void PopulateXrefAll(IDataClient dataClient, string customer, string project)
        {

            var projectConfig = _iam.GetProjectConfig(customer, project);

            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_result_schema", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema },
                };

            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_populate_xref_all", parameters);
            dataClient.ExecuteScalar<object>(request);

            _logging.Log("usp_populate_xref_all proc executed");

        }

        private async Task UpdateApplications(ICloudFoundryClient cloudFoundryClient, List<String> applicationNames, string status)
        {

            var appListTasks = applicationNames.Select(applicationName => new ApplicationRequest
            {
                Resource = new Resource
                {
                    Entity = new Entity
                    {
                        Name = applicationName,
                        State = status
                    }
                }
            }).Select(applicationRequest => JsonConvert.SerializeObject(applicationRequest, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            })).Select(applicationRequestJson => cloudFoundryClient.Client.PutAsync(string.Empty,
                                                                                    new StringContent(applicationRequestJson, Encoding.UTF8, "application/json")))
                                                                                    .ToList();


            // Wait asynchronously for all of them to finish
            await Task.WhenAll(appListTasks);

            _validation.ValidateResponses(appListTasks.Select(x => x.Result).ToList());

        }

        private void UpdateStatus(string customerShortName, string projectShortName, Guid id, ApplicationType applicationType, string taProgressPercentage)
        {

            var filter = JsonConvert.SerializeObject(new { _id = id, clientName = customerShortName, projectName = projectShortName });
            var projection = "{\"flowchartRunRequest.flowchartRunUUID\" : 1}";

            var jobs = _jobProxy.GetJobs(filter, projection);
            _logging.Log($"The progress of {applicationType} is {taProgressPercentage} from status api");

            var progressPercentage = _config.PreTAPercentageContribution + (int)Math.Round((double)(Convert.ToInt32(taProgressPercentage) * _config.TAPercentageContribution) / 100);
            _logging.Log($"The progress of {applicationType} is {progressPercentage} for XL UI");

            // Update status and summary only if the progress has changed
            if (HasProgressChanged(customerShortName, projectShortName, id, progressPercentage))
            {
                var analyticsSummary = GetSummary(customerShortName, projectShortName, id, applicationType);

                var workStatusMessages = CreateWorkStatusMessages(analyticsSummary, taProgressPercentage, progressPercentage);
                var listWorkStatus = new List<WorkStatus>();

                foreach (var orchestrationJob in jobs) // Update status and summary for both analytics run uuid and flowchart run uuids
                {

                    var analyticsWorkStatus = new WorkStatus(orchestrationJob.analyticsRunUUID, true, progressPercentage, AnalyticsRunStatus.Running);

                    analyticsWorkStatus.LoggableMessages = workStatusMessages;
                    listWorkStatus.Add(analyticsWorkStatus);

                    orchestrationJob.flowchartRunRequest.ToList().ForEach(x =>
                    {

                        var flowchartWorkStatus = new WorkStatus(x.flowchartRunUUID, progressPercentage, AnalyticsRunStatus.Running);

                        flowchartWorkStatus.LoggableMessages = workStatusMessages;
                        listWorkStatus.Add(flowchartWorkStatus);

                    });
                }

                _workStatusProxy.Add(listWorkStatus);

            }

        }

        private bool HasProgressChanged(string customerShortName, string projectShortName, Guid id, int progressPercentage)
        {

            var lastStatus = _workStatusProxy.GetLastStatus(customerShortName, projectShortName, id, ViewLevel.All, 1);
            if (lastStatus == null) { return false; }

            _logging.Log($"Current progress percentage - {progressPercentage}; Previous progress - {lastStatus.Progress}");
            return progressPercentage > lastStatus.Progress;

        }

        private AnalyticsSummary GetSummary(string customerShortName, string projectShortName, Guid id, ApplicationType applicationType)
        {

            dynamic client = null;

            switch (applicationType)
            {
                case ApplicationType.TA:
                    client = HttpContext.RequestServices.GetService(typeof(ITAOrchestratorClient));
                    break;
                case ApplicationType.EventSink:
                    client = HttpContext.RequestServices.GetService(typeof(IEventSinkClient));
                    break;
                case ApplicationType.FlowchartSink:
                    client = HttpContext.RequestServices.GetService(typeof(IFlowchartSinkClient));
                    break;
            }

            var analyticsSummary = new AnalyticsRunSummary(client, _validation).GetRunSummary(customerShortName, projectShortName, id);
            return analyticsSummary;

        }

        private List<WorkStatusMessage> CreateWorkStatusMessages(AnalyticsSummary analyticsSummary, string taProgressPercentage, int progressPercentage)
        {

            var workMessages = new List<WorkStatusMessage>();

            var memberSummaryContent =
                $@"Members Processed: {analyticsSummary.MembersProcessed}, 
                   Total Members: {analyticsSummary.TotalMembers }, 
                   Failed Members: {analyticsSummary.FailedMembers}, 
                   Skipped Members: {analyticsSummary.SkippedMembers }.";

            _logging.Log($"XL ui ta progress - {taProgressPercentage}; Status api ta progress - {progressPercentage};");
            _logging.Log(memberSummaryContent);

            // Customer level member summary message
            var customerMemberSummaryMessage = new WorkStatusMessage
            {
                Content = memberSummaryContent,
                Severity = TraceEventType.Information,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Customer
            };

            workMessages.Add(customerMemberSummaryMessage);

            // Internal member and claim/event summary work messages
            var internalMemberSummaryMessage = new WorkStatusMessage
            {
                Content = memberSummaryContent,
                Severity = TraceEventType.Information,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
            };

            workMessages.Add(internalMemberSummaryMessage);

            var claimEventContent = $@"Claims Processed: {analyticsSummary.ClaimsProcessed}, Events Generated: {analyticsSummary.EventsGenerated}.";
            var internalClaimEventWorkMessage = new WorkStatusMessage
            {
                Content = claimEventContent,
                Severity = TraceEventType.Information,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
            };

            workMessages.Add(internalClaimEventWorkMessage);
            return workMessages;
        }

        #endregion

        #region Public Methods

        [HttpPost]
        public IActionResult CancelFlowchartRun(Guid flowchartRunId)
        {

            var listWorkStatus = new List<WorkStatus>();
            var status = new WorkStatus(flowchartRunId, false, 100, AnalyticsRunStatus.Canceled);

            status.LoggableMessages.Add(new WorkStatusMessage()
            {
                Content = "Cancelled by user.",
                Severity = TraceEventType.Information,
                Timestamp = DateTime.Now,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
            });

            listWorkStatus.Add(status);
            _workStatusProxy.Add(listWorkStatus);

            return Ok();

        }

        [HttpPost]
        public IActionResult CancelFlowchartRuns([FromServices] IStonebranchClient stonebranchClient, string customerShortName, string projectShortName, Guid jobId, string workflowInstanceId)
        {

            var stoneBranch = new Stonebranch(stonebranchClient, _validation);
            var tasks = stoneBranch.GetCurrentTasks(workflowInstanceId, "*_CacheDB__*");

            var filter = JsonConvert.SerializeObject(new { _id = jobId, clientName = customerShortName, projectName = projectShortName });
            var projection = "{\"flowchartRunRequest.flowchartRunUUID\" : 1}";
            var jobs = _jobProxy.GetJobs(filter, projection);

            var listWorkStatus = new List<WorkStatus>();

            foreach (var orchestrationJob in jobs) // Update status for both analytics run uuid and flowchart run uuids
            {
                orchestrationJob.flowchartRunRequest.ToList().ForEach(x =>
                {

                    if (!tasks.Any(y => y.Name.Contains(x.flowchartRunUUID.ToString()) && y.Status == "SUCCESS"))
                    {

                        var status = new WorkStatus(x.flowchartRunUUID, false, 100, AnalyticsRunStatus.Canceled);

                        status.LoggableMessages.Add(new WorkStatusMessage()
                        {
                            Content = "Cancelled by user.",
                            Severity = TraceEventType.Information,
                            Timestamp = DateTime.Now,
                            ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
                        });

                        listWorkStatus.Add(status);

                    }

                });
            }

            _workStatusProxy.Add(listWorkStatus);
            return Ok();

        }

        /// <summary>
        /// Fails a run.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="workflowName">workflow name</param>
        /// <param name="workflowInstanceId">workflow instance id</param>
        /// <param name="taskName">task name</param>
        /// <param name="jobId">analytics run id</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult FailRun([FromServices] IStonebranchClient stonebranchClient, string customerShortName, string projectShortName, string workflowInstanceId, string workflowName, string taskName, Guid jobId)
        {

            var workflow = ParseWorkflow(workflowName);
            var prevStatus = _workStatusProxy.GetLastStatus(customerShortName, projectShortName, jobId);

            var filter = JsonConvert.SerializeObject(new { _id = jobId, clientName = customerShortName, projectName = projectShortName });
            var projection = "{\"flowchartRunRequest.flowchartRunUUID\" : 1}";
            var jobs = _jobProxy.GetJobs(filter, projection);

            switch (workflow)
            {
                case WorkflowType.AnalyticsRun: // fail analytics run and fail all the flowchart runs in the analytics run

                    var statuses = new List<WorkStatus>();

                    statuses.Add(new WorkStatus(jobId, true, prevStatus.Progress, AnalyticsRunStatus.Running));

                    if (taskName != _config.FinishTask)
                    {
                        jobs.ToList().ForEach(job => job.flowchartRunRequest.ToList().ForEach(flowchartRun =>
                        {
                            statuses.Add(new WorkStatus(flowchartRun.flowchartRunUUID, prevStatus.Progress, AnalyticsRunStatus.Running));
                        }));
                    }

                    _workStatusProxy.Add(statuses);
                    break;

                case WorkflowType.CacheDB: // just fail flowchart run if in cachedb

                    var flowchartRunId = Guid.Parse(workflowName.Substring(workflowName.IndexOf("_CacheDB__") + 10));
                    _workStatusProxy.Add(new WorkStatus(flowchartRunId, prevStatus.Progress, AnalyticsRunStatus.Running));

                    var stoneBranch = new Stonebranch(stonebranchClient, _validation);
                    var tasks = stoneBranch.GetCurrentTasks(workflowInstanceId);

                    var requests = jobs.First().flowchartRunRequest;
                    var index = requests.IndexOf(requests.FirstOrDefault(x => x.flowchartRunUUID == flowchartRunId));

                    if (index == requests.Count() - 1)
                    {
                        var finishTask = tasks.FirstOrDefault(x => x.Name == _config.FinishTask);
                        stoneBranch.ClearPredecessors(finishTask.Id);
                    }
                    else
                    {

                        var nextRun = requests[index + 1];
                        var nextTask = tasks.FirstOrDefault(x => x.Name.Contains(nextRun.flowchartRunUUID.ToString()));

                        stoneBranch.ClearPredecessors(nextTask.Id);

                    }

                    break;

                default:
                    _logging.Log($"Run failed status not processed due to failure to parse workflow name to type: { workflowName }", Orchestration.Shared.Domain.Log.LogLevels.Error);
                    break;

            }

            return Ok();

        }

        [HttpPost]
        public IActionResult FinishCancel(string customerShortName, string projectShortName, Guid jobId)
        {

            var runStatus = new WorkStatus(jobId, false, 100, AnalyticsRunStatus.Canceled);

            runStatus.LoggableMessages.Add(new WorkStatusMessage()
            {
                Content = "Cancelled by user.",
                Severity = TraceEventType.Information,
                Timestamp = DateTime.Now,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Customer
            });

            _workStatusProxy.Add(runStatus);
            Guid cancelJobId;

            if (Guid.TryParse(_workStatusProxy.GetWorkProperty(customerShortName, jobId, "cancel_job_id"), out cancelJobId))
            {
                _workStatusProxy.Add(new WorkStatus(cancelJobId, 100, AnalyticsRunStatus.RanToCompletion));
            }

            return Ok();

        }

        /// <summary>
        /// Finishes a flowchart or analytics run.
        /// </summary>
        /// <param name="id">analytics or flowchart run id to finish</param>
        /// <param name="workflowInstanceId">workflow instance id</param>
        /// <param name="workflowName">name of workflow</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult FinishRun([FromServices] IStonebranchClient stonebranchClient, Guid id, string workflowInstanceId, string workflowName)
        {

            var workflow = ParseWorkflow(workflowName);
            var status = string.Empty;

            switch (workflow)
            {
                case WorkflowType.AnalyticsRun:

                    var stonebranch = new Stonebranch(stonebranchClient, _validation);
                    var tasks = stonebranch.GetCurrentTasks(workflowInstanceId);

                    if (tasks.Any(x => x.Type == "Workflow" && x.Status == "RUNNING/PROBLEMS"))
                    {
                        status = "There are CacheDB workflows that did not finish successfully."; // ae status did by fail run above
                    }
                    else
                    {
                        _workStatusProxy.Add(new WorkStatus(id, 100, AnalyticsRunStatus.RanToCompletion));
                        status = "finished";
                    }

                    break;

                case WorkflowType.CacheDB:

                    _workStatusProxy.Add(new WorkStatus(id, 100, AnalyticsRunStatus.RanToCompletion));
                    status = "finished";

                    break;

                default:
                    _logging.Log($"Run finished status not processed due to failure to parse workflow name to type: { workflowName }", Orchestration.Shared.Domain.Log.LogLevels.Error);
                    break;
            }

            return Ok(status);

        }

        /// <summary>
        /// Checks to see if an analytics run is complete.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        /// <param name="application">Application Name</param>
        /// <returns>true or false</returns>
        [HttpGet]
        public IActionResult IsFinished(string customerShortName, string projectShortName, Guid id, string application)
        {

            ApplicationType applicationType;

            if (!Enum.TryParse(application, out applicationType))
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    $"Application Type '{application}' is not supported. The supported Application Types are: " +
                    $"{string.Join(", ", Enum.GetValues(typeof(ApplicationType)).Cast<ApplicationType>())}");
            }

            dynamic client = null;

            switch (applicationType)
            {
                case ApplicationType.TA:
                    client = HttpContext.RequestServices.GetService(typeof(ITAOrchestratorClient));
                    break;
                case ApplicationType.EventSink:
                    client = HttpContext.RequestServices.GetService(typeof(IEventSinkClient));
                    break;
                case ApplicationType.FlowchartSink:
                    client = HttpContext.RequestServices.GetService(typeof(IFlowchartSinkClient));
                    break;
                case ApplicationType.BatchEventBuild:
                    client = HttpContext.RequestServices.GetService(typeof(IBatchEventBuildOchestratorClient));
                    break;
                case ApplicationType.DischargeSink:
                    client = HttpContext.RequestServices.GetService(typeof(IBatchDischargeBuildOrchestratorClient));
                    break;
            }

            var analyticsStatus = new AnalyticsRunStatusProxy(client, _validation, _logging).GetRunStatus(customerShortName, projectShortName, id);

            // Update status/progress and summary to Analytics Engine DB. WorkStatus/Progress and summary reported to Analytics Engine DB 
            // is for now only dependent on TA's status/progress.
            if (applicationType == ApplicationType.TA || applicationType == ApplicationType.DischargeSink)
            {
                UpdateStatus(customerShortName, projectShortName, id, applicationType, analyticsStatus.percentage);
            }

            var status = analyticsStatus.status.Equals("End", StringComparison.OrdinalIgnoreCase);
            return Ok(status);

        }

        /// <summary>
        /// Manages TA Containers in Cloud Foundry
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="state"></param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> ManageTAContainers([FromServices] ICloudFoundryClient cloudFoundryClient, string customerShortName, string projectShortName, string state)
        {

            ApplicationState applicationState;

            if (!Enum.TryParse(state, out applicationState))
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    $"Application State '{state}' is not supported. The supported Application States are: " +
                    $"{string.Join(", ", Enum.GetValues(typeof(ApplicationState)).Cast<ApplicationState>())}");
            }

            // TA Applications are deployed in '{customerShortName}_{projectShortName}-{ApplicationName}' format in PCF
            var taApplications = _config.TAApplications.Select(applicationName => string.Format($"{customerShortName}_{projectShortName}-{applicationName}")).ToList();

            await UpdateApplications(cloudFoundryClient, taApplications, state);
            return Ok();

        }

        /// <summary>
        /// Starts an anlytics run.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        [HttpPut]
        public IActionResult Start([FromServices] IBatchAnalyticsClient batchAnalyticsClient, string customerShortName, string projectShortName, Guid id)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, _config.PreTAPercentageContribution, true, true);

            var json = JsonConvert.SerializeObject(job);
            var response = batchAnalyticsClient.Client.PostAsync("analyticsrun", new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);
            return Ok();

        }

        /// <summary>
        /// Starts an anlytics run.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        [HttpPut]
        public IActionResult UpdateAnalyticsRunPercent(string customerShortName, string projectShortName, Guid id, int percentage)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, percentage, true, true);

            return Ok();

        }


        [HttpPut]
        public IActionResult DeleteAnalyticsRunGreenplum([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid analyticsRunId)
        {

            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);
            var parameters = new List<NpgsqlParameter>
                {
                    new NpgsqlParameter("v_source_schema_name", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.SourceSchema },
                    new NpgsqlParameter("v_result_schema_name", NpgsqlDbType.Varchar) { Value = projectConfig.GreenplumConfig.ResultSchema },
                    new NpgsqlParameter("v_flowchart_run_uuid", NpgsqlDbType.Varchar) { Value = analyticsRunId.ToString() }
                };

            var request = new GreenplumStoredProcedureRequest(projectConfig.GreenplumConfig.RawConnectionString, "usp_delete_flowchart_run", parameters);
            dataClient.ExecuteScalar<object>(request);

            _logging.Log("Deleted rates from greenplum", Orchestration.Shared.Domain.Log.LogLevels.Info, 50);
            return Ok();

        }

        [HttpPut]
        public IActionResult DeleteAnalyticsRunReportingDb([FromServices] IReportingServicesClient reportingServicesClient, string customerShortName, string projectShortName, Guid analyticsRunId)
        {

            var input = new
            {
                customerShortName = customerShortName,
                projectShortName = projectShortName,
                flowchartRunId = analyticsRunId,
            };

            var json = JsonConvert.SerializeObject(input);
            var method = "DeleteFlowchartRun_Rest";
            var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);

            var result = response.Content.ReadAsStringAsync().Result;
            var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

            if (!succeeded)
            {
                throw new OperationCanceledException($"Delete rates from reporting db failed for the following input: api: {method}, json: {json}");
            }

            _logging.Log("Deleted rates from reporting db", Orchestration.Shared.Domain.Log.LogLevels.Info, 100);
            return Ok();

        }

        [HttpPost]
        public IActionResult isWorkflowComplete([FromServices] IStonebranchClient stonebranchClient, string workflowInstanceId)
        {

            var stonebranch = new Stonebranch(stonebranchClient, _validation);
            var workflowTasks = stonebranch.GetCurrentTasks(workflowInstanceId);
            var activeTasks = workflowTasks.Where(t => t.Status != "FINISHED" && t.Status != "SUCCESS" && t.Status != "SKIPPED");

            return Ok(activeTasks.Count() == 0);

        }

        [HttpPost]
        public IActionResult isWorkflowSuccess([FromServices] IStonebranchClient stonebranchClient, string workflowInstanceId)
        {

            var stonebranch = new Stonebranch(stonebranchClient, _validation);
            var workflowTasks = stonebranch.GetCurrentTasks(workflowInstanceId);

            return Ok(!workflowTasks.Any(task => task.Status != "SUCCESS" && task.Status != "SKIPPED"));

        }


        /// <summary>
        /// Exports event definitions
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        [HttpPut]
        public IActionResult MREPlusEventDefinitions([FromServices] IDataExtractionClient dataExtractionClient, string customerShortName, string projectShortName, Guid id)
        {
            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            var fileNameSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");
            var mrePlusEnabled = false;
            if (job.eventMrePlusOptions != null && Boolean.TryParse(job.eventMrePlusOptions.Value, out mrePlusEnabled) && mrePlusEnabled)
            {
                bool folderCreated = CreateFolder(dataExtractionClient, customerShortName, projectShortName, id);
                if (folderCreated)
                {

                    job.eventMrePlusOptions.file_name_suffix = fileNameSuffix;
                    bool isgZip = job.eventMrePlusOptions != null && job.eventMrePlusOptions.archive_format.ToLower() == "gzip";
                    var input = new
                    {
                        customerShortName = customerShortName,
                        projectShortName = projectShortName,
                        folderName = id.ToString() + @"/" + _config.MrePlusEventsFolderName,
                        fileType = $"TAB|txt{(isgZip ? ".gz" : string.Empty)}",
                        fileNameSuffix = fileNameSuffix
                    };

                    var json = JsonConvert.SerializeObject(input);
                    var method = "MassResultsExportPlusEventDefinitions_Rest";
                    var response = dataExtractionClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                    _validation.ValidateResponse(response);

                    var result = response.Content.ReadAsStringAsync().Result;
                    var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

                    if (!succeeded)
                    {
                        throw new OperationCanceledException($"MRE+ Event definition export failed for the following input: api: {method}, json: {json}");
                    }

                    _jobProxy.UpdateJob(job);

                    _logging.Log("MREPlusEventDefinitions completed", Orchestration.Shared.Domain.Log.LogLevels.Info);
                }
                else
                {
                    _logging.Log("CreateMrePlusEventFolders_Rest failed", Orchestration.Shared.Domain.Log.LogLevels.Info);
                }
            }
            else
            {
                _logging.Log("Skipped running MREPlusEventDefinitions", Orchestration.Shared.Domain.Log.LogLevels.Info);
            }

            return Ok();

        }

        private bool CreateFolder(IDataExtractionClient dataExtractionClient, string customerShortName, string projectShortName, Guid id)
        {
            var exportFolderRoot = _iam.GetProjectConfig(customerShortName, projectShortName).MassResultExportConfig;
            if (!Directory.Exists(Path.Combine(exportFolderRoot.ExportLandingZone, customerShortName, projectShortName, id.ToString(), _config.MrePlusEventsFolderName)))
            {
                var input = new
                {
                    customerShortName = customerShortName,
                    projectShortName = projectShortName,
                    folderName = id.ToString(),
                };

                var json = JsonConvert.SerializeObject(input);
                var method = "CreateMrePlusEventFolders_Rest";
                var response = dataExtractionClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                _validation.ValidateResponse(response);

                var result = response.Content.ReadAsStringAsync().Result;
                var property = JObject.Parse(result).Property("CreateMrePlusEventFolders_RestResult");
                return property.Value.ToObject<bool>();
            }
            else
            {
                return true;
            }

        }


        /// <summary>
        /// Exports event definitions
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">job id</param>
        [HttpPut]
        public IActionResult MREPlusXref([FromServices] IDataExtractionClient dataExtractionClient, [FromServices] IDataClient dataClient, string customerShortName, string projectShortName, Guid id)
        {
            var job = _jobProxy.GetJob(customerShortName, projectShortName, id);

            var mrePlusEnabled = false;
            if (job.eventMrePlusOptions != null && Boolean.TryParse(job.eventMrePlusOptions.Value, out mrePlusEnabled) && mrePlusEnabled)
            {

                bool folderCreated = CreateFolder(dataExtractionClient, customerShortName, projectShortName, id);
                bool isgZip = job.eventMrePlusOptions != null && job.eventMrePlusOptions.archive_format.ToLower() == "gzip";

                if (folderCreated)
                {

                    PopulateXrefAll(dataClient, customerShortName, projectShortName);
                    var input = new
                    {
                        customerShortName = customerShortName,
                        projectShortName = projectShortName,
                        folderName = id.ToString() + @"/" + _config.MrePlusEventsFolderName,
                        fileType = $"TAB|txt{(isgZip ? ".gz" : string.Empty)}",
                        fileNameSuffix = job.eventMrePlusOptions.file_name_suffix
                    };

                    var json = JsonConvert.SerializeObject(input);
                    var method = "MassResultsExportPlusXrefs_Rest";
                    var response = dataExtractionClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

                    _validation.ValidateResponse(response);

                    var result = response.Content.ReadAsStringAsync().Result;
                    var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

                    if (!succeeded)
                    {
                        throw new OperationCanceledException($"MRE+ Xref export failed for the following input: api: {method}, json: {json}");
                    }

                    _logging.Log("MassResultsExportPlusXrefs completed", Orchestration.Shared.Domain.Log.LogLevels.Info);
                }
                else
                {
                    _logging.Log("CreateMrePlusEventFolders_Rest failed", Orchestration.Shared.Domain.Log.LogLevels.Info);
                }
            }
            else
            {
                _logging.Log("Skipped running MassResultsExportPlusXrefs", Orchestration.Shared.Domain.Log.LogLevels.Info);
            }

            return Ok();

        }


        [HttpPut]
        public IActionResult Vacuum([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, string entity)
        {

            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var vacuumRequest = new Vacuum(projectConfig.GreenplumConfig.RawConnectionString, entity, false, projectConfig.GreenplumConfig.ResultSchema);
            var result = dataClient.RunOptimization(vacuumRequest);

            return Ok(result);

        }

        [HttpPut]
        public IActionResult VacuumAnalyze([FromServices] IDataClient dataClient, string customerShortName, string projectShortName, string entity)
        {

            var projectConfig = _iam.GetProjectConfig(customerShortName, projectShortName);

            var vacuumRequest = new Vacuum(projectConfig.GreenplumConfig.RawConnectionString, entity, true, projectConfig.GreenplumConfig.ResultSchema);
            var result = dataClient.RunOptimization(vacuumRequest);

            return Ok(result);

        }

        [HttpPost]
        public IActionResult LogMreMessage([FromBody]MreMessage mreMessage)
        {
            var workMessages = new List<WorkStatusMessage>();

            _logging.Log($"Logging MRE Message with Content: {mreMessage.Content}, JobID: {mreMessage.JobId},", Orchestration.Shared.Domain.Log.LogLevels.Info);

            var logMreSummaryInternalMessage = new WorkStatusMessage
            {
                Content = mreMessage.Content,
                Severity = TraceEventType.Information,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
            };
            workMessages.Add(logMreSummaryInternalMessage);

            if (!mreMessage.IsError)
            {
                var logMreSummaryCustomerMessage = new WorkStatusMessage
                {
                    Content = mreMessage.Content,
                    Severity = TraceEventType.Information,
                    ViewLevelProperty = WorkStatusMessage.ViewLevel.Customer
                };
                workMessages.Add(logMreSummaryCustomerMessage);
            }


            var logMreWorkStatus = new WorkStatus(mreMessage.JobId, 0, AnalyticsRunStatus.Running)
            {
                LoggableMessages = workMessages
            };

            _workStatusProxy.Add(logMreWorkStatus);
            _logging.Log($"Successfully logged Mre Message for the JobId: {mreMessage.JobId}", Orchestration.Shared.Domain.Log.LogLevels.Info);

            return Ok(true);

        }

        #endregion

        #region IsBatchRunSuccessful
        /// <summary>
        /// Method to get sucessful status based on the configured batch fail threshold limit percentage
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>
        /// <param name="id">Analytics run uuid</param>
        /// <param name="application">application name</param>
        /// <returns>bool: success status</returns>
        [HttpGet]
        public IActionResult IsBatchRunSuccessful(string customerShortName, string projectShortName, Guid id, string application)
        {
            ApplicationType applicationType;

            if (!Enum.TryParse(application, out applicationType))
            {
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    $"Application Type '{application}' is not supported. The supported Application Types are: " +
                    $"{string.Join(", ", Enum.GetValues(typeof(ApplicationType)).Cast<ApplicationType>())}");
            }

            var analyticsSummary = GetSummary(customerShortName, projectShortName, id, applicationType);
            var percentageOfFailure = (decimal)analyticsSummary.FailedMembers / analyticsSummary.TotalMembers * 100 ;
            return Ok(percentageOfFailure < _config.BatchFailThresholdPercentageLimit);
        }
        #endregion
        
    }
}
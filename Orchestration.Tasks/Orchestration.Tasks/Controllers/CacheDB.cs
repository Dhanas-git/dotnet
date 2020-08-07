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
using Orchestration.Backbone.Domain;
using Orchestration.Shared;
using Orchestration.Shared.Orchestrator;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using static Orchestration.Tasks.Models.CacheModel;

namespace Orchestration.Tasks.Controllers
{

    public class CacheDBController : Controller
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

        public CacheDBController(IAppConfig config,
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

        private void AddTasks(IStonebranchClient stonebranchClient, IOrchestrationJob job, string workflowInstanceId, string finishTaskId)
        {

            int vertexX = 200;
            int vertexY = 450;

            var stonebranch = new Stonebranch(stonebranchClient, _validation);
            var tasks = stonebranch.GetCurrentTasks(workflowInstanceId, $"*{_config.CacheDBTaskFilter}*");
            var predecessor = _config.CacheDBPredecessor;
            var flowchartRunCount = job.flowchartRunRequest.Count;
            var i = 1;

            job.flowchartRunRequest.ToList().ForEach(flowchart =>
            {

                var alias = $"{job.clientName}_{job.projectName}_CacheDB__{flowchart.flowchartRunUUID}";
                var successors = new List<String>();

                if (i == flowchartRunCount)
                {
                    successors.Add(_config.FinishTask);
                }

                if (!tasks.Any(x => x.Name == alias))
                {

                    var data = new TaskInsert()
                    {
                        Alias = alias,
                        Name = "CacheDB",
                        Predecessors = new List<String>() { predecessor },
                        Successors = successors,
                        VertexX = vertexX,
                        VertexY = vertexY,
                        WorkflowInstanceId = workflowInstanceId
                    };

                    var connection = new Uri(new Uri(_config.StonebranchUri), "taskinstance/ops-task-insert");
                    var xml = data.ToXml();
                    var content = new StringContent(xml, Encoding.UTF8, "application/xml");

                    var response = stonebranchClient.Client.PostAsync(connection.ToString(), content).Result;

                    _validation.ValidateResponse(response);
                    _validation.ValidateStonebranchResponse(response.Content.ReadAsStringAsync().Result);

                    if (i == flowchartRunCount)
                    {
                        stonebranch.ReleaseTask(workflowInstanceId, finishTaskId);
                    }

                    predecessor = alias;
                    vertexX += 200;

                }

                i++;

            });

        }

        private int GetPercentage(Operations operation)
        {

            switch (operation)
            {
                case Operations.GenerateMemberMonthInfo:
                    return _config.PreCacheDBPercentageContribution;
                case Operations.GenerateHybridIdssReport:
                case Operations.GenerateHybridRates:
                    return Calculations.Percentage(_config.PreCacheDBPercentageContribution, 1, 8, 100);
                case Operations.GenerateRates:
                    return Calculations.Percentage(_config.PreCacheDBPercentageContribution, 2, 8, 100);
                case Operations.GenerateSupplementalSummaryByFileType:
                case Operations.GenerateSupplementalSummaryReport:
                case Operations.GenerateSupplementalSummaryReportByTable:
                    return Calculations.Percentage(_config.PreCacheDBPercentageContribution, 4, 8, 100);
                case Operations.GenerateTableMeasureReport:
                    return Calculations.Percentage(_config.PreCacheDBPercentageContribution, 5, 8, 100);
                default:
                    throw new InvalidOperationException("Invalid operation");
            }

        }

        private string GetServiceMethod(Operations operation)
        {
            return $"{operation.ToString()}_Rest";
        }

        private void RunOperation(IReportingServicesClient reportingServicesClient, RatesCacheModel model, bool reRunRateGeneration)
        {

            var json = JsonConvert.SerializeObject(model);
            var method = GetServiceMethod(model.operation);

            var fallback = _config.CacheDBPercentageContribution;
            var stopWatch = Stopwatch.StartNew();
            var response = reportingServicesClient.Client.PostAsync(method, new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);

            if (!reRunRateGeneration)
            {
                _taskLogging.LogOperation
                (
                    model.customerShortName,
                    model.projectShortName,
                    model.operation.ToString(),
                    model.flowchartRunId,
                    stopWatch.Elapsed,
                    fallback
                );
            }

            var result = response.Content.ReadAsStringAsync().Result;
            var succeeded = ((JProperty)JObject.Parse(result).First).Value.Value<bool>();

            if (!succeeded)
            {
                throw new OperationCanceledException($"Run failed for the following input: method: {method}, json: {json}");
            }

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and injects CacheDB workflow tasks into workflow.
        /// </summary>
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project Short name</param>
        /// <param name="workflowInstanceId">workflow instance id</param>
        /// <param name="jobId">job id</param>
        [HttpPut]
        public ActionResult InjectTasks([FromServices] IStonebranchClient stonebranchClient, string customerShortName, string projectShortName, string workflowInstanceId, string finishTaskId, Guid jobId)
        {

            var job = _jobProxy.GetJob(customerShortName, projectShortName, jobId);
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, 0, true, true);
            AddTasks(stonebranchClient, job, workflowInstanceId, finishTaskId);
            return Ok();

        }

        /// <summary>
        /// Runs selected operation on cachedb for one flowchart run in job.
        /// </summary>		
        /// <param name="customerShortName">customer short name</param>
        /// <param name="projectShortName">project short name</param>		
        /// <param name="jobId">job id</param>
        /// <param name="flowchartRunId">flowchart run id</param>
        /// <param name="operation">operation to run</param>
        [HttpPut]
        public ActionResult Run
        (
            [FromServices] IReportingServicesClient reportingServicesClient,
            string customerShortName,
            string projectShortName,
            Guid jobId,
            Guid flowchartRunId,
            Operations operation,
            bool reRunRateGeneration = false
        )
        {
            var job = _jobProxy.GetJob(customerShortName, projectShortName, jobId);
            var flowchartRun = job.flowchartRunRequest.FirstOrDefault(x => x.flowchartRunUUID == flowchartRunId);

            if (flowchartRun == null) { throw new NullReferenceException("The flowchart run was not present in the job."); }
            if (!reRunRateGeneration)
                _workStatusProxy.Add(new WorkStatus(flowchartRunId, false, GetPercentage(operation), AnalyticsRunStatus.Running));

            if (!reRunRateGeneration && operation == Operations.GenerateMemberMonthInfo)
            {

                var index = job.flowchartRunRequest.IndexOf(flowchartRun);

                if (index > 0)
                {
                    var percentage = Calculations.Percentage(_config.PreCacheDBPercentageContribution, index, job.flowchartRunRequest.Count, 100);
                    _workStatusProxy.Add(new WorkStatus(jobId, false, percentage, AnalyticsRunStatus.Running));
                }
                else
                {

                    _workStatusProxy.Add(new WorkStatus(jobId, false, _config.PreCacheDBPercentageContribution, AnalyticsRunStatus.Running));

                    for (int i = 1; i < job.flowchartRunRequest.Count; i++)
                    {
                        _workStatusProxy.Add(new WorkStatus(job.flowchartRunRequest[i].flowchartRunUUID, false, GetPercentage(operation), AnalyticsRunStatus.Running));
                    }

                }

            }

            var model = new RatesCacheModel()
            {
                customerShortName = customerShortName,
                flowchartRunId = flowchartRunId,
                flowchartCatalogPopulations = flowchartRun.flowchartCatalogPopulations,
                operation = operation,
                populationIds = (job.populationIds == null || job.populationIds.Count == 0) ? null : job.populationIds, // passing an empty list causes it to not populate
                projectShortName = projectShortName,
                hrSampleMeasureIDs = flowchartRun.hrSampleMeasureMetadata.Keys.ToList()
            };

            if (!SkipOperation(model, operation))
            {
                RunOperation(reportingServicesClient, model, reRunRateGeneration);
            }
            return Ok();

        }

        /// <summary>
        /// Invokes cachedb workflow
        /// </summary>
        /// <param name="stonebranchClient"></param>
        /// <param name="customerShortName"></param>
        /// <param name="projectShortName"></param>
        /// <param name="id"></param>
        /// <param name="flowchartRunId"></param>
        /// <returns></returns>
        [HttpPut]
        public ActionResult InvokeCacheDBWorkFlow
        (
            [FromServices] IStonebranchClient stonebranchClient,
            string customerShortName,
            string projectShortName,
            Guid jobId,
            Guid flowchartRunId,
            string workFlowInstanceId
        )
        {


            var stonebranch = new Stonebranch(stonebranchClient, _validation);

            var alias = $"{customerShortName}_{projectShortName}_CacheDB__{flowchartRunId}";
            var predecessor = "Call_CacheDB_Workflow";
            var data = new TaskInsert()
            {
                Alias = alias,
                Name = "CacheDB",
                WorkflowInstanceId = workFlowInstanceId,
                Predecessors = new List<String>() { predecessor },
                Successors = new List<String>() { }
            };

            var connection = new Uri(new Uri(_config.StonebranchUri), "taskinstance/ops-task-insert");
            var xml = data.ToXml();
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");

            var response = stonebranchClient.Client.PostAsync(connection.ToString(), content).Result;

            _validation.ValidateResponse(response);
            _validation.ValidateStonebranchResponse(response.Content.ReadAsStringAsync().Result);

            return Ok();

        }


        /// <summary>
        /// Validations to decide if we can skip the task
        /// </summary>
        /// <param name="model"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        private bool SkipOperation(RatesCacheModel model, Operations operation)
        {
            bool skip = false;
            switch (operation)
            {

                case Operations.GenerateMemberMonthInfo:
                    break;
                case Operations.GenerateHybridIdssReport:
                    skip = model.hrSampleMeasureIDs.Count == 0;
                    break;
                case Operations.GenerateHybridRates:
                    skip = model.hrSampleMeasureIDs.Count == 0;
                    break;
                case Operations.GenerateRates:
                    break;
                case Operations.GenerateSupplementalSummaryByFileType:
                    break;
                case Operations.GenerateSupplementalSummaryReport:
                    break;
                case Operations.GenerateSupplementalSummaryReportByTable:
                    break;
                case Operations.GenerateTableMeasureReport:
                    break;
                default:
                    break;
            }
            return skip;
        }

        #endregion

    }
}

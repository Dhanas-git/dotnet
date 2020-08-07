#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using Orchestration.Backbone.Domain;
using Orchestration.Shared;
using System.Diagnostics;
using Orchestration.Shared.AnalyticsEngine;


namespace Orchestration.Tasks.Models
{
    /// <summary>
    /// Provides common methods to make it easier to communicate with the job repository.
    /// </summary>
    public class TaskLogging : ITaskLogging
    {

        #region Enumerations

        public enum RunType
        {
            Analytics,
            FlowchartRun
        }

        #endregion
        
        private ILogging _logging;
        private IValidation _validation;
        private IWorkStatusProxy _workStatusProxy;

        public TaskLogging(ILogging logging, IValidation validation, IWorkStatusProxy workStatusProxy)
        {            
            _logging = logging;
            _validation = validation;
            _workStatusProxy = workStatusProxy;
        }

        #region Private Methods

        private void LogOperationPCF(Guid id, RunType typeOfRun, string message)
        {
            var idString = $"{(typeOfRun == RunType.Analytics ? "Analytics Run Job" : "Flowchart Run Job")} : {id}";
            _logging.Log($"{idString}, {message}");
        }

        #endregion

        #region Public Methods

        public void LogOperation(string customerShortName, 
                                 string projectShortName, 
                                 string operation,
                                 Guid flowchartRunId, 
                                 TimeSpan span,
                                 int fallbackProgress)
        {
            
            var lastStatus = _workStatusProxy.GetLastStatus(customerShortName, projectShortName, flowchartRunId);
            var progress = lastStatus != null ? lastStatus.Progress : fallbackProgress;

            var content = $"Operation { operation.ToString() } finished with a time of: {span.ToString("c")}.";
            var status = new WorkStatus(flowchartRunId, progress, AnalyticsRunStatus.Running);

            status.LoggableMessages.Add(new WorkStatusMessage()
            {
                Content = content,
                Severity = TraceEventType.Information,
                Timestamp = DateTime.Now,
                ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
            });

            _workStatusProxy.Add(status);
            LogOperationPCF(flowchartRunId, RunType.FlowchartRun, content);

        }

        public void LogOperation(string customerShortName, 
                                 string projectShortName, 
                                 string operation, 
                                 IOrchestrationJob job, 
                                 TimeSpan span,
                                 int fallbackProgress = 0,
                                 bool writeChildren = true,
                                 bool canCancel = true)
        {
            

            var lastStatus = _workStatusProxy.GetLastStatus(customerShortName, projectShortName, job.analyticsRunUUID);
            var progress = lastStatus != null ? lastStatus.Progress : fallbackProgress;

            var content = $"Operation {operation} finished with a time of: {span.ToString("c")}.";

            var messages = new List<WorkStatusMessage>() {
                new WorkStatusMessage()
                {
                    Content = content,
                    Severity = TraceEventType.Information,
                    Timestamp = DateTime.Now,
                    ViewLevelProperty = WorkStatusMessage.ViewLevel.Internal
                 }
            };
            
            _workStatusProxy.Add(job, AnalyticsRunStatus.Running, messages, progress, writeChildren, canCancel);
            LogOperationPCF(job.analyticsRunUUID, RunType.Analytics, content);

        }
    }

    #endregion

}

using System;
using System.Collections.Generic;
using Orchestration.Backbone.Domain;
using Orchestration.Shared.AnalyticsEngine;

namespace Orchestration.Tasks.Models
{
    public interface IWorkStatusProxy
    {
        void Add(IOrchestrationJob job, AnalyticsRunStatus taskStatus, int progress, bool writeChildren, bool canCancel);
        void Add(IOrchestrationJob job, AnalyticsRunStatus taskStatus, List<WorkStatusMessage> messages, int progress, bool writeChildren, bool canCancel);
        void Add(List<WorkStatus> statuses);
        void Add(WorkStatus workStatus);
        WorkStatus GetLastStatus(string customerShortName, string projectShortName, Guid workId, ViewLevel viewLevel = ViewLevel.All, int maxMessages = 10);
        Dictionary<string, string> GetWorkProperties(string customerShortName, Guid workId);
        string GetWorkProperty(string customerShortName, Guid workId, string property);
    }
}
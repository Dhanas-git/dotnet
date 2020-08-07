using System;
using Orchestration.Backbone.Domain;

namespace Orchestration.Tasks.Models
{
    public interface ITaskLogging
    {
        void LogOperation(string customerShortName, string projectShortName, string operation, Guid flowchartRunId, TimeSpan span, int fallbackProgress);
        void LogOperation(string customerShortName, string projectShortName, string operation, IOrchestrationJob job, TimeSpan span, int fallbackProgress = 0, bool writeChildren = true, bool canCancel = true);
    }
}
using System;
using System.Collections.Generic;
using Orchestration.Backbone.Domain;

namespace Orchestration.Tasks.Models
{
    public interface IJobProxy
    {
        IOrchestrationJob GetJob(string customerShortName, string projectShortName, Guid id);
        IEnumerable<IOrchestrationJob> GetJobs(string filter, string projection);
        void UpdateJob(IOrchestrationJob job);
    }
}
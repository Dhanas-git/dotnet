using System;
using Orchestration.Backbone.Domain;

namespace Orchestration.Tasks.Models
{
    public interface IAnalyticsRunStatusProxy
    {
        AnalyticsStatus GetRunStatus(string customerShortName, string projectShortName, Guid id);
    }
}
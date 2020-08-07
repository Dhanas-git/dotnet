using System;
using Orchestration.Backbone.Domain;

namespace Orchestration.Tasks.Models
{
    public interface IAnalyticsRunSummary
    {
        AnalyticsSummary GetRunSummary(string customerShortName, string projectShortName, Guid id);
    }
}
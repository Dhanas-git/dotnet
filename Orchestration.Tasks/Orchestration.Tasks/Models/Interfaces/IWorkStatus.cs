using System;
using System.Collections.Generic;
using Orchestration.Shared.AnalyticsEngine;
using static Orchestration.Tasks.Models.WorkStatus;

namespace Orchestration.Tasks.Models
{
    public interface IWorkStatus
    {
        bool CanCancel { get; set; }
        Guid Id { get; set; }
        List<WorkStatusMessage> LoggableMessages { get; set; }
        List<WorkStatusMessage> LoggedMessages { get; set; }
        int Progress { get; set; }
        AnalyticsRunStatus TaskStatus { get; set; }
    }
}
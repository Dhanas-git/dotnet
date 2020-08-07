using System;
using System.Collections.Generic;

namespace Orchestration.Tasks.Models
{
    public interface ICacheModel
    {
        string customerShortName { get; set; }
        Guid flowchartRunId { get; set; }
        CacheModel.Operations operation { get; set; }
        List<int> populationIds { get; set; }
        string projectShortName { get; set; }

        
    }
}
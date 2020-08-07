using System.Collections.Generic;

namespace Orchestration.Tasks.Models
{
    public interface IRatesCacheModel
    {
        string flowchartCatalogPopulations { get; set; }

        /// <summary>
        /// For admin rate generation
        /// </summary>
        List<int> hrSampleMeasureIDs { get; set; }
    }
}
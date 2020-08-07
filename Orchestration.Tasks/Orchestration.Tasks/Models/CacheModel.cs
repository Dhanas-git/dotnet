#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Orchestration.Tasks.Models
{
    public class CacheModel : ICacheModel
    {

		#region Public Enumerations

		/// <summary>
		/// Operations that can be perform for cacheing db
		/// </summary>
		public enum Operations
		{			
			GenerateMemberMonthInfo,
            GenerateHybridIdssReport,
            GenerateHybridRates,
            GenerateRates,
			GenerateSupplementalSummaryByFileType,
			GenerateSupplementalSummaryReport,
			GenerateSupplementalSummaryReportByTable,
			GenerateTableMeasureReport,

		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets or sets customer short name
		/// </summary>
		public string customerShortName { get; set; }

		/// <summary>
		/// Gets or sets project short name.
		/// </summary>
		public string projectShortName { get; set; }

		/// <summary>
		/// Gets or sets flowchart run id.
		/// </summary>
		public Guid flowchartRunId { get; set; }        

		/// <summary>
		/// Gets or sets population ids.
		/// </summary>
		public List<int> populationIds { get; set; }

		/// <summary>
		/// Gets or sets the operation to run.
		/// </summary>
		[JsonIgnore]
		public Operations operation { get; set; }

        

        #endregion

    }
}

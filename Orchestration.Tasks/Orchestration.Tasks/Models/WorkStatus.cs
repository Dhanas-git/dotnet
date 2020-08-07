#region Copyright © 2018 Inovalon

//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//

#endregion

using System;
using System.Collections.Generic;
using Orchestration.Shared.AnalyticsEngine;

namespace Orchestration.Tasks.Models
{
	public class WorkStatus : IWorkStatus
    {		     

        #region Public Properties

        public Guid Id { get; set; }

		public bool CanCancel { get; set; }

		public int Progress { get; set; }

		public List<WorkStatusMessage> LoggableMessages { get; set; }

		public List<WorkStatusMessage> LoggedMessages { get; set; }

		public AnalyticsRunStatus TaskStatus { get; set; }

		#endregion

		#region Public Constructors
        
        public WorkStatus()
        {
        }

		public WorkStatus(Guid id, int progress, AnalyticsRunStatus taskStatus)
			: this(id, false, progress, taskStatus)
		{			
		}

		public WorkStatus(Guid id, bool canCancel, int progress, AnalyticsRunStatus taskStatus)            
		{

			Id = id;
			CanCancel = canCancel;
			Progress = progress;
			TaskStatus = taskStatus;

			LoggableMessages = new List<WorkStatusMessage>();

		}

		#endregion		

	}
}

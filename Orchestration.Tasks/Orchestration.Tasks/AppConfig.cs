#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Linq;

namespace Orchestration.Tasks
{

    /// <summary>
    /// Provides access to all the configuraton variables for the environment.
    /// </summary>
    public class AppConfig : IAppConfig
    {        

        #region Public Properties

        /// <summary>
        /// Gets the api url for the analytics engine workflow service.
        /// </summary>
        public string AnalyticsEngineUri => Environment.GetEnvironmentVariable("AnalyticsEngineUri");

        /// <summary>
        /// Gets the api url for batch analytics service.
        /// </summary>
        public string BatchAnalyticsUri => Environment.GetEnvironmentVariable("BatchAnalyticsUri");

        /// <summary>
        /// Gets the api url for batch analytics service.
        /// </summary>
        public string TAOrchestrationUri => Environment.GetEnvironmentVariable("TAOrchestrationUri");

        /// <summary>
        /// Contains the method used in Status API
        /// </summary>
        public string StatusAPIMethod => Environment.GetEnvironmentVariable("StatusAPIMethod");

        /// <summary>
        /// Contains the method used in Status API
        /// </summary>
        public string SummaryAPIMethod => Environment.GetEnvironmentVariable("SummaryAPIMethod");

        /// <summary>
		/// Gets the percentage contribution of CacheDB tasks in Analytics Run process
		/// </summary>
		public int CacheDBPercentageContribution
        {
            get
            {
                Int32.TryParse(Environment.GetEnvironmentVariable("CacheDBPercentageContribution"), out var cacheDBPercentageContribution);
                return cacheDBPercentageContribution;
            }
        }

        /// <summary>
        /// Gets the predecessor to the cachdb workflow tasks.
        /// </summary>
        public string CacheDBPredecessor
        {
            get { return Environment.GetEnvironmentVariable("CacheDBPredecessor"); }
        }

        /// <summary>
		/// Gets the cachedb filter to use to delete tasks.
		/// </summary>
		public string CacheDBTaskFilter
        {
            get { return Environment.GetEnvironmentVariable("CacheDBTaskFilter"); }
        }

        /// <summary>
        /// Gets the api url for cloud foundry client application.
        /// </summary>
        public string CfClientUri => Environment.GetEnvironmentVariable("CfClientUri");

        public int ClientAnalyticsEngineTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientAnalyticsEngineTimeout"));
        public int ClientBatchAnalyticsTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientBatchAnalyticsTimeout"));
        public int ClientCloudFoundryTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientCloudFoundryTimeout"));
        public int ClientEventSinkTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientEventSinkTimeout"));
        public int ClientFlowchartSinkTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientFlowchartSinkTimeout"));
        public int ClientIAMCustomerManagementTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientIAMCustomerManagementTimeout"));
        public int ClientJsonManagerTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientJsonManagerTimeout"));
        public int ClientReportingServicesTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientReportingServicesTimeout"));
        public int ClientRouterTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientRouterTimeout"));
        public int ClientStonebranchTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientStonebranchTimeout"));
        

        /// <summary>
        /// Gets the api url for event sink status service.
        /// </summary>
        public string EventSinkStatusUri => Environment.GetEnvironmentVariable("EventSinkStatusUri");

        /// <summary>
        /// Gets the api url for event sink status service.
        /// </summary>
        public string FinishTask => Environment.GetEnvironmentVariable("FinishTask");

        /// <summary>
        /// Gets the api url for flowchart sink status service.
        /// </summary>
        public string FlowchartSinkStatusUri => Environment.GetEnvironmentVariable("FlowchartSinkStatusUri");

        /// <summary>
        /// Gets the api url for the json manager service.
        /// </summary>
        public string JsonManagerUri => Environment.GetEnvironmentVariable("JsonManagerUri");

        /// <summary>
		/// Gets the IAM api url for customer management.
		/// </summary>
		public string IAMCustomerManagementUri
        {
            get { return Environment.GetEnvironmentVariable("IAMCustomerManagementUri"); }
        }

        /// <summary>
        /// Gets the percentage contribution of Pre-CacheDB tasks in Analytics Run process
        /// </summary>
        public int PreCacheDBPercentageContribution
        {
            get
            {
                return 100 - CacheDBPercentageContribution;
            }
        }

        /// <summary>
        /// Gets the percentage contribution of Pre-TA tasks in Analytics Run process
        /// </summary>
        public int PreTAPercentageContribution
        {
            get
            {
                Int32.TryParse(Environment.GetEnvironmentVariable("PreTAPercentageContribution"), out var preTAPercentageContribution);
                return preTAPercentageContribution;
            }
        }

        /// <summary>
		/// Gets the api url for the big data reporting service.
		/// </summary>
		public string ReportingServicesUri
        {
            get { return Environment.GetEnvironmentVariable("ReportingServicesUri"); }
        }

        public string RouterUri => Environment.GetEnvironmentVariable("RouterUri");

        /// <summary>
        /// Gets the password for stonebranch.
        /// </summary>
        public string StonebranchPassword => Environment.GetEnvironmentVariable("StonebranchPassword");

        /// <summary>
        /// Gets the user for stonebranch.
        /// </summary>
        public string StonebranchUser => Environment.GetEnvironmentVariable("StonebranchUser");

        /// <summary>
        /// Gets the api url for stonebranch.
        /// </summary>
        public string StonebranchUri => Environment.GetEnvironmentVariable("StonebranchUri");      

        /// <summary>
        /// Gets the list of TA applications
        /// </summary>
        public string[] TAApplications => Environment.GetEnvironmentVariable("TAApplications")?.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        /// <summary>
        /// Gets the percentage contribution of TA in Analytics Run process
        /// </summary>
        public int TAPercentageContribution
        {
            get
            {
                Int32.TryParse(Environment.GetEnvironmentVariable("TAPercentageContribution"), out var taPercentageContribution);
                return taPercentageContribution;
            }
        }

        public string BatchEventBuildOrchestrationUri => Environment.GetEnvironmentVariable("BatchEventBuildOrchestrationUri");
        public string DischargeSinkStatusUri => Environment.GetEnvironmentVariable("DischargeSinkStatusUri");

        public string DataExtractionUri => Environment.GetEnvironmentVariable("DataExtractionUri");

        public string MrePlusEventsFolderName => Environment.GetEnvironmentVariable("MrePlusEventsFolderName");

        public string MrePlusDischargesFolderName => Environment.GetEnvironmentVariable("MrePlusDischargesFolderName");

        public int ClientDataExtractionTimeout => Int32.Parse(Environment.GetEnvironmentVariable("ClientDataExtractionTimeout"));

        public int BatchFailThresholdPercentageLimit => Int32.Parse(Environment.GetEnvironmentVariable("BatchFailThresholdPercentageLimit"));
        #endregion

    }

}

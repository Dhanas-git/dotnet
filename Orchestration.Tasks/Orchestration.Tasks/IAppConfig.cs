namespace Orchestration.Tasks
{
    public interface IAppConfig
    {
        string AnalyticsEngineUri { get; }
        string BatchAnalyticsUri { get; }
        int CacheDBPercentageContribution { get; }
        string CacheDBPredecessor { get; }
        string CacheDBTaskFilter { get; }
        string CfClientUri { get; }
        int ClientAnalyticsEngineTimeout { get; }
        int ClientBatchAnalyticsTimeout { get; }
        int ClientCloudFoundryTimeout { get; }
        int ClientEventSinkTimeout { get; }
        int ClientFlowchartSinkTimeout { get; }
        int ClientIAMCustomerManagementTimeout { get; }        
        int ClientJsonManagerTimeout { get; }
        int ClientReportingServicesTimeout { get; }
        int ClientRouterTimeout { get; }
        int ClientStonebranchTimeout { get; }
        string EventSinkStatusUri { get; }
        string DischargeSinkStatusUri { get; }
        string FinishTask { get; }
        string FlowchartSinkStatusUri { get; }
        string IAMCustomerManagementUri { get; }
        string JsonManagerUri { get; }
        int PreCacheDBPercentageContribution { get; }
        int PreTAPercentageContribution { get; }
        string ReportingServicesUri { get; }
        string RouterUri { get; }
        string StonebranchPassword { get; }
        string StonebranchUri { get; }
        string StonebranchUser { get; }
        string[] TAApplications { get; }
        int TAPercentageContribution { get; }
        int ClientDataExtractionTimeout { get; }
        string DataExtractionUri { get; }
        string MrePlusEventsFolderName { get; }
        string MrePlusDischargesFolderName { get; }
        int BatchFailThresholdPercentageLimit { get; }
    }
}
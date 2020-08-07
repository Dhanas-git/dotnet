using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Orchestration.Backbone.Domain;
using Orchestration.Data;
using Orchestration.Data.Models;
using Orchestration.Shared;
using Orchestration.Shared.Domain.IAM;
using Orchestration.Tasks.Controllers;
using Orchestration.Tasks.Models;
using Orchestration.Tasks.Test.Helpers;
using Orchestration.Tasks.Test.Mocks;
using System;
using Xunit;

namespace Orchestration.Tasks.Test
{
    public class Metadata_Test
    {

        /// <summary>
        /// Standard test, should work.
        /// </summary>
        [Theory, AutoMoqData]
        public void PublishGreenplum_Test(Mock<IAppConfig> config,
                                          Mock<IJobProxy> jobProxy,
                                          Mock<ILogging> logging,
                                          Mock<ITaskLogging> taskLogging,
                                          Mock<IValidation> validation,
                                          Mock<IWorkStatusProxy> workStatusProxy,
                                          IOrchestrationJob orchestrationJob,                                         
                                          string customerShortName,
                                          string projectShortName,
                                          Guid id)

        {

            var fixture = new Fixture();

            config.SetupGet(x => x.IAMCustomerManagementUri).Returns(fixture.Create<Uri>().ToString());
            config.SetupGet(x => x.AnalyticsEngineUri).Returns(fixture.Create<Uri>().ToString());
           
            jobProxy.Setup(x => x.GetJob(customerShortName, projectShortName, id)).Returns(orchestrationJob);

            var metaData = new MetadataController(config.Object, jobProxy.Object, logging.Object, taskLogging.Object, validation.Object, workStatusProxy.Object);
            var result = metaData.PublishGreenplum(new MockReportingServicesClient("{'PublishFlowchartMetadataGreenPlum_RestResult':true}"), customerShortName, projectShortName, id);

            result.Should().BeOfType<OkResult>();

        }

        /// <summary>
        /// Standard test, should work.
        /// </summary>
        [Theory, AutoMoqData]
        public void PublishSQL_Test(Mock<IAppConfig> config,
                                    Mock<IJobProxy> jobProxy,
                                    Mock<ILogging> logging,
                                    Mock<ITaskLogging> taskLogging,
                                    Mock<IValidation> validation,
                                    Mock<IWorkStatusProxy> workStatusProxy,
                                    IOrchestrationJob orchestrationJob,                                         
                                    string customerShortName,
                                    string projectShortName,
                                    Guid id)

        {

            var fixture = new Fixture();

            config.SetupGet(x => x.IAMCustomerManagementUri).Returns(fixture.Create<Uri>().ToString());
            config.SetupGet(x => x.AnalyticsEngineUri).Returns(fixture.Create<Uri>().ToString());  
            
            jobProxy.Setup(x => x.GetJob(customerShortName, projectShortName, id)).Returns(orchestrationJob);

            var metaData = new MetadataController(config.Object, jobProxy.Object, logging.Object, taskLogging.Object, validation.Object, workStatusProxy.Object);
            var result = metaData.PublishSql(new MockReportingServicesClient("{'PublishFlowchartMetadataReportingDB_RestResult':true}"), customerShortName, projectShortName, id);

            result.Should().BeOfType<OkResult>();

        }

        /// <summary>
        /// Standard test, should work.
        /// </summary>       
        [Theory]
        [InlineAutoMoqData(1)]
        public void TestLongRunning_Test(int timeout,
                                             Mock<IIAM> iam,
                                             Mock<IAppConfig> config,
                                             Mock<IDataClient> dataClient,
                                             Mock<IJobProxy> jobProxy,
                                             Mock<IStoredProcedureRequest> greenplumRequest,
                                             Mock<ILogging> logging,
                                             Mock<ITaskLogging> taskLogging,
                                             Mock<IValidation> validation,
                                             Mock<IWorkStatusProxy> workStatusProxy,
                                             IOrchestrationJob orchestrationJob,
                                             ProjectConfig projectConfig,
                                             string customerShortName,
                                             string projectShortName,
                                             Guid id)
        {

            var fixture = new Fixture();

            config.SetupGet(x => x.IAMCustomerManagementUri).Returns(fixture.Create<Uri>().ToString());
            projectConfig.GreenplumConfig.RawConnectionString = Utilities.ValidConnectionString;

            dataClient.Setup(x => x.ExecuteScalar<object>(greenplumRequest.Object)).Returns(It.IsAny<object>());            
            iam.Setup(x => x.GetProjectConfig(customerShortName, projectShortName)).Returns(projectConfig);

            var metaData = new MetadataController(config.Object, jobProxy.Object, logging.Object, taskLogging.Object, validation.Object, workStatusProxy.Object);
            var result = metaData.TestLongRunning(dataClient.Object, iam.Object, customerShortName, projectShortName, timeout);

            result.Should().BeOfType<OkResult>();

        }

    }

}

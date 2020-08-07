using AutoFixture;
using Moq;
using Newtonsoft.Json;
using Orchestration.Backbone.Domain;
using Orchestration.Shared;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Controllers;
using Orchestration.Tasks.Models;
using Orchestration.Tasks.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Orchestration.Tasks.Test
{
    public class SetupRun_Test
    {
        /// <summary>
        /// Should fail if there is no router key for update sample
        /// </summary>
        [Theory, AutoMoqData]
        public void IsUpdateSampleStatusFinished_Should_Fail_If_No_RouterKey(Mock<IAppConfig> config,
                                          Mock<IJobProxy> jobProxy,
                                          Mock<ILogging> logging,
                                          Mock<IIAM> iam,
                                          Mock<ITaskLogging> taskLogging,
                                          Mock<IValidation> validation,
                                          Mock<IWorkStatusProxy> workStatusProxy,
                                          IOrchestrationJob orchestrationJob,
                                          Mock<IRouterClient> routerClient,
                                          string customerShortName,
                                          string projectShortName,
                                          Guid id)

        {

            var fixture = new Fixture();

            config.SetupGet(x => x.IAMCustomerManagementUri).Returns(fixture.Create<Uri>().ToString());
            config.SetupGet(x => x.AnalyticsEngineUri).Returns(fixture.Create<Uri>().ToString());

            jobProxy.Setup(x => x.GetJob(customerShortName, projectShortName, id)).Returns(orchestrationJob);

            var setupRun = new SetupRunController(config.Object, jobProxy.Object, iam.Object, logging.Object, taskLogging.Object, validation.Object, workStatusProxy.Object);

            Assert.Throws<InvalidOperationException>(() => setupRun.IsUpdateSampleStatusFinished(routerClient.Object, customerShortName, projectShortName, id, orchestrationJob.flowchartRunRequest.First().flowchartRunUUID));

        }

        /// <summary>
        /// Should fail if the json value for request ids is invalid 
        /// </summary>
        [Theory, AutoMoqData]
        public void IsUpdateSampleStatusFinished_Should_Fail_If_Invalid_Json(Mock<IAppConfig> config,
                                          Mock<IJobProxy> jobProxy,
                                          Mock<ILogging> logging,
                                          Mock<IIAM> iam,
                                          Mock<ITaskLogging> taskLogging,
                                          Mock<IValidation> validation,
                                          Mock<IWorkStatusProxy> workStatusProxy,
                                          IOrchestrationJob orchestrationJob,
                                          Mock<IRouterClient> routerClient,
                                          string customerShortName,
                                          string projectShortName,
                                          Guid id)

        {

            var fixture = new Fixture();
            var runId = orchestrationJob.flowchartRunRequest.First().flowchartRunUUID;
            config.SetupGet(x => x.IAMCustomerManagementUri).Returns(fixture.Create<Uri>().ToString());
            config.SetupGet(x => x.AnalyticsEngineUri).Returns(fixture.Create<Uri>().ToString());
            orchestrationJob.lastRouterRequests.Add($"UpdateSampleStatus_{runId.ToString()}", Guid.NewGuid().ToString());
            jobProxy.Setup(x => x.GetJob(customerShortName, projectShortName, id)).Returns(orchestrationJob);

            var setupRun = new SetupRunController(config.Object, jobProxy.Object, iam.Object, logging.Object, taskLogging.Object, validation.Object, workStatusProxy.Object);

            Assert.Throws<JsonReaderException>(() => setupRun.IsUpdateSampleStatusFinished(routerClient.Object, customerShortName, projectShortName, id, orchestrationJob.flowchartRunRequest.First().flowchartRunUUID));

        }


        
    }
}

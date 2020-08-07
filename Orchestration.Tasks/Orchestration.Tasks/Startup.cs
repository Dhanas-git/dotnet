#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Orchestration.Backbone.Domain;
using Orchestration.Data;
using Orchestration.Shared;
using Orchestration.Shared.Domain.IAM;
using Orchestration.Shared.Handlers;
using Orchestration.Shared.Orchestrator;
using Orchestration.Tasks.Clients;
using Orchestration.Tasks.Models;
using System.Collections.Generic;

namespace Orchestration.Tasks
{
    public class Startup
    {

        private static ImpliedImplementationInterfaceSerializer<IList<T>, List<T>> ListOfInterfacesSerializer<T, TI>() where TI : class, T
        {

            var serializer = new ImpliedImplementationInterfaceSerializer<T, TI>();
            var listSerializer = new EnumerableInterfaceImplementerSerializer<List<T>, T>(serializer);

            return new ImpliedImplementationInterfaceSerializer<IList<T>, List<T>>(listSerializer);

        }

        public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{           

            services.Configure<CookiePolicyOptions>(options => // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            {                
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc(opts => {
                opts.Filters.Add<AutoLogAttribute>();
                opts.Filters.Add<ExceptionFilter>();
			});

            BsonClassMap.RegisterClassMap<OrchestrationJob>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(x => x.flowchartRunRequest).SetSerializer(ListOfInterfacesSerializer<IFlowchartRunRequest, FlowchartRunRequest>());
            });

            BsonClassMap.RegisterClassMap<FlowchartRunRequest>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(x => x.flowchartCatalogMetadata).SetSerializer(ListOfInterfacesSerializer<IFlowchartCatalogMetadata, FlowchartCatalogMetadata>());
            });

            BsonClassMap.RegisterClassMap<FlowchartCatalogMetadata>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(x => x.eventCatalogs).SetSerializer(ListOfInterfacesSerializer<IEventCatalog, EventCatalog>());
                cm.MapMember(x => x.promptVariables).SetSerializer(ListOfInterfacesSerializer<IPromptVariable, PromptVariable>());
            });

            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationHandler>();

            services.AddEnchancedHttpClient<IIAMClient, IAMClient>();
            services.AddEnchancedHttpClient<IAnalyticsEngineClient, AnalyticsEngineClient>();
            services.AddEnchancedHttpClient<IDataExtractionClient, DataExtractionClient>();
            services.AddEnchancedHttpClient<IBatchAnalyticsClient, BatchAnalyticsClient>();
            services.AddEnchancedHttpClient<ITAOrchestratorClient, TAOrchestratorClient>();
            services.AddEnchancedHttpClient<IBatchEventBuildOchestratorClient, BatchEventBuildOrchestratorClient>();
            services.AddEnchancedHttpClient<ICloudFoundryClient, CloudFoundryClient>();
            services.AddEnchancedHttpClient<IEventSinkClient, EventSinkClient>();
            services.AddEnchancedHttpClient<IFlowchartSinkClient, FlowchartSinkClient>();
            services.AddEnchancedHttpClient<IJsonManagerClient, JsonManagerClient>();
            services.AddEnchancedHttpClient<IReportingServicesClient, ReportingServicesClient>();
            services.AddEnchancedHttpClient<IStonebranchClient, StonebranchClient>();
            services.AddEnchancedHttpClient<IRouterClient, RouterClient>();
            services.AddEnchancedHttpClient<IBatchDischargeBuildOrchestratorClient, BatchDischargeBuildOrchestratorClient>();

            services.AddScoped<IDataClient, DataClient>();
            services.AddScoped<IIAM, IAM>();
            services.AddScoped<IJobProxy, JobProxy>();
            services.AddScoped<ITaskLogging, TaskLogging>();
            services.AddScoped<IWorkStatusProxy, WorkStatusProxy>();
            
            services.AddSingleton<IAppConfig, AppConfig>();
            services.AddSingleton<ICertificateManager, CertificateManager>();
            services.AddSingleton<ILogging, Logging>();
            services.AddSingleton<IValidation, Validation>();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);                

        }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{

            new Logging(null).LogEnvironmentVariables();

            var certManager = app.ApplicationServices.GetService<ICertificateManager>();

            ServiceCollectionExtensions.CertificateManager = certManager;
            certManager.LoadCerts();

            app.UseHttpsRedirection();
            app.UseCookiePolicy(); 

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}");
            });

        }

	}
}

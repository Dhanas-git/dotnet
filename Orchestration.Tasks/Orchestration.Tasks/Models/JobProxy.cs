#region Copyright © 2017 Inovalon
//
// All rights are reserved. Reproduction or transmission in whole or in part, in
// any form or by any means, electronic, mechanical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
//
#endregion

using Newtonsoft.Json;
using Orchestration.Backbone.Domain;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Orchestration.Shared;
using Orchestration.Tasks.Clients;
using MongoDB.Bson.Serialization;

namespace Orchestration.Tasks.Models
{
    /// <summary>
    /// Provides common methods to make it easier to communicate with the job repository.
    /// </summary>
    public class JobProxy : IJobProxy
    {

        private readonly Uri _address;

        private IJsonManagerClient _jsonManagerClient;
        private IValidation _validation;

        /// <summary>
        /// Initializes an instance of the proxy.
        /// </summary>
        /// <param name="jsonManagerAddress">address of json manager service to use</param>
        public JobProxy(IAppConfig config, IJsonManagerClient jsonManagerClient, IValidation validation)
        {

            _address = new Uri(config.JsonManagerUri);

            _jsonManagerClient = jsonManagerClient;
            _validation = validation;

        }

        /// <summary>
        /// Gets the OrchestrationJob that matches input criteria from job repository 
        /// </summary>
        /// <param name="customerShortName">Client Name</param>
        /// <param name="projectShortName">Project Name</param>
        /// <param name="id"></param>
        /// <returns></returns>
        public IOrchestrationJob GetJob(string customerShortName, string projectShortName, Guid id)
        {

            var uri = new Uri(_address, $"get?customerShortName={customerShortName}&projectShortName={projectShortName}&id={id}");
            var response = _jsonManagerClient.Client.GetAsync(uri.ToString()).Result;

            _validation.ValidateResponse(response);
            var job = JsonConvert.DeserializeObject<OrchestrationJob>(response.Content.ReadAsStringAsync().Result);

            if(job == null) { throw new InvalidOperationException($"Unable to retrieve a job with id: {id}."); }
            return job;

        }

        /// <summary>
        /// Gets Orchestration Jobs that match filter and projection criteria from the job repository 
        /// </summary>
        /// <param name="filter">https://docs.mongodb.com/getting-started/shell/query/</param>
        /// <param name="projection">https://docs.mongodb.com/manual/reference/operator/aggregation/project/</param>
        /// <returns>Returns Orchestration Jobs that match filter and projection criteria from the job repository</returns>
        public IEnumerable<IOrchestrationJob> GetJobs(string filter, string projection)
        {            

            var uri = new Uri(_address, $"find?filter={filter}&projection={projection}");            
            var response = _jsonManagerClient.Client.GetAsync(uri.ToString()).Result;

            _validation.ValidateResponse(response);

            var result = response.Content.ReadAsStringAsync().Result;
            var json = result;

            if(result.StartsWith('"')) { json = JsonConvert.DeserializeObject<string>(result); } // old json manager backward compatibility
            return BsonSerializer.Deserialize<IList<OrchestrationJob>>(json);

        }

        /// <summary>
        /// Updates an orchestration job in the job repository.
        /// </summary>
        /// <param name="job">job to update</param>
        public void UpdateJob(IOrchestrationJob job)
        {

            var uri = new Uri(_address, "put");
            var json = JsonConvert.SerializeObject(job);
            var response = _jsonManagerClient.Client.PutAsync(uri.ToString(), new StringContent(json, Encoding.UTF8, "application/json")).Result;

            _validation.ValidateResponse(response);

        }

    }

}

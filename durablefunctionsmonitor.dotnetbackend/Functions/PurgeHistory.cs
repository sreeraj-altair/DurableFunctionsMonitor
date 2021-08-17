using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using DurableTask.Core;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DurableFunctionsMonitor.DotNetBackend
{
    public static class PurgeHistory
    {
        // Request body
        class PurgeHistoryRequest
        {
            public string TimeFrom { get; set; }
            public string TimeTill { get; set; }
            public OrchestrationStatus[] Statuses { get; set; }
            public EntityTypeEnum EntityType { get; set; }
        }

        // Purges orchestration instance history
        // POST /a/p/i/{taskHubName}/purge-history
        [FunctionName(nameof(DfmPurgeHistoryFunction))]
        public static async Task<IActionResult> DfmPurgeHistoryFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = Globals.ApiRoutePrefix + "/purge-history")] HttpRequest req,
            [DurableClient(TaskHub = Globals.TaskHubRouteParamName)] IDurableClient durableClient, 
            ILogger log)
        {
            // Checking that the call is authenticated properly
            try
            {
                await Auth.ValidateIdentityAsync(req.HttpContext.User, req.Headers, durableClient.TaskHubName);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to authenticate request");
                return new UnauthorizedResult();
            }

            // Checking that we're not in ReadOnly mode
            if (DfmEndpoint.Settings.Mode == DfmMode.ReadOnly)
            {
                log.LogError("Endpoint is in ReadOnly mode");
                return new StatusCodeResult(403);
            }

            // Important to deserialize time fields as strings, because otherwise time zone will appear to be local
            var request = JsonConvert.DeserializeObject<PurgeHistoryRequest>(await req.ReadAsStringAsync());

            var result = request.EntityType == EntityTypeEnum.DurableEntity ?
                await durableClient.PurgeDurableEntitiesHistory(DateTime.Parse(request.TimeFrom),
                    DateTime.Parse(request.TimeTill)) :
                await durableClient.PurgeOrchestrationsHistory(DateTime.Parse(request.TimeFrom),
                    DateTime.Parse(request.TimeTill), request.Statuses);

            return result.ToJsonContentResult();
        }


        [FunctionName(nameof(DfmPurgeQuoteContextFunction))]
        public static async Task<IActionResult> DfmPurgeQuoteContextFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Globals.ApiRoutePrefix + "/purge-quote-context")] HttpRequest req,
            [DurableClient(TaskHub = Globals.TaskHubRouteParamName)] IDurableClient durableClient,
            ILogger log
            )
        {
            //Checking that the call is authenticated properly
            try
            {
                await Auth.ValidateIdentityAsync(req.HttpContext.User, req.Headers, durableClient.TaskHubName);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to authenticate request");
                return new UnauthorizedResult();
            }


            string connectionString = Environment.GetEnvironmentVariable(EnvVariableNames.AzureWebJobsStorage);
            var blobClient = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();
            
            // Delete the container
            var container = blobClient.GetContainerReference("quotecontext");
            await container.DeleteAsync();

            Thread.Sleep(40000);

            // Create the container again 
            var newContainer = blobClient.GetContainerReference("quotecontext");
            bool result = await newContainer.CreateIfNotExistsAsync();
            if (result)
            {
                return new OkResult();
            }

            return new BadRequestResult();
        }

        [FunctionName(nameof(DfmPurgeDocGenerationContextFunction))]
        public static async Task<IActionResult> DfmPurgeDocGenerationContextFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Globals.ApiRoutePrefix + "/purge-docgen-context")] HttpRequest req,
            [DurableClient(TaskHub = Globals.TaskHubRouteParamName)] IDurableClient durableClient,
            ILogger log
            )
        {
            //Checking that the call is authenticated properly
            try
            {
                await Auth.ValidateIdentityAsync(req.HttpContext.User, req.Headers, durableClient.TaskHubName);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to authenticate request");
                return new UnauthorizedResult();
            }


            string connectionString = Environment.GetEnvironmentVariable(EnvVariableNames.AzureWebJobsStorage);
            var blobClient = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();

            // Delete the container
            var container = blobClient.GetContainerReference("docgeneration");
            await container.DeleteAsync();

            Thread.Sleep(40000);

            // Create the container again 
            var newContainer = blobClient.GetContainerReference("docgeneration");
            bool result = await newContainer.CreateIfNotExistsAsync();
            if (result)
            {
                return new OkResult();
            }

            return new BadRequestResult();
        }

        private static Task<PurgeHistoryResult> PurgeOrchestrationsHistory(
            this IDurableClient durableClient, 
            DateTime timeFrom, 
            DateTime timeTill, 
            OrchestrationStatus[] statuses)
        {
            return durableClient.PurgeInstanceHistoryAsync(timeFrom, timeTill, statuses);
        }

        private static async Task<PurgeHistoryResult> PurgeDurableEntitiesHistory(
            this IDurableClient durableClient,
            DateTime timeFrom,
            DateTime timeTill)
        {
            var query = new EntityQuery
            {
                LastOperationFrom = timeFrom,
                LastOperationTo = timeTill
            };

            int instancesDeleted = 0;
            EntityQueryResult response = null;
            do
            {
                query.ContinuationToken = response == null ? null : response.ContinuationToken;

                response = durableClient.ListEntitiesAsync(query, CancellationToken.None).Result;
                foreach (var entity in response.Entities)
                {
                    await durableClient.PurgeInstanceHistoryAsync(entity.EntityId.ToString());
                    instancesDeleted++;
                }
            }
            while (!string.IsNullOrEmpty(response.ContinuationToken));

            return new PurgeHistoryResult(instancesDeleted);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using newshub.types;
using newshub.functions.utils;

namespace newshub.functions;

public static class Getter
{
    private static int totalRecords = 0;

    [FunctionName("GetAll")]
    public static async Task<IActionResult> GetAll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/all/{limit:int}/{offset:int}")] HttpRequest  req,
        ILogger log, 
        int limit, 
        int offset)
    {
        if (limit <= 0 || offset < 0)
        {
            return new BadRequestObjectResult("Invalid input parameters");
        }

        string cacheKey = "totalRecords";

        Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

        if (!CacheManager.Instance.TryGetValue(cacheKey, out totalRecords))
        {
            try
            {
                var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
                var countResponse = await container.GetItemQueryIterator<int>(countQuery).ReadNextAsync();
                totalRecords = countResponse.FirstOrDefault();
                CacheManager.Instance.Set(cacheKey, totalRecords, TimeSpan.FromMinutes(10));
            }
            catch (Exception e)
            {
                log.LogError($"Error executing count query: {e.Message}");
                return new StatusCodeResult(500); 
            }
        }

        try
        {
            var query = new QueryDefinition("SELECT * FROM c ORDER BY c.publishedAt DESC OFFSET @offset LIMIT @limit")
            .WithParameter("@offset", offset * limit)
            .WithParameter("@limit", limit);

            var response = await container.GetItemQueryIterator<Article>(query).ReadNextAsync();
            var articles = response.ToList();
            var result = new
            {
                TotalRecords = totalRecords,
                Articles = articles
            };

            return new OkObjectResult(result);
        }
        catch (Exception e)
        {
            log.LogError($"Error executing main query: {e.Message}");
            return new StatusCodeResult(500); 
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json;
using newshub.types;
using newshub.functions.utils;

namespace newshub.functions;

public static class HttpTrigger
{
    [FunctionName("GetArticle")]
    public static IActionResult GetArticle([HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/get/{id}")] HttpRequest req, ILogger log, string id)
    {
        try 
        {
            Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

            Article article = container.GetItemLinqQueryable<Article>(true).Where(a => a.Id == id).AsEnumerable().FirstOrDefault();
        
            if (article != null)
            {
                return new OkObjectResult(article);
            }

            return new NotFoundResult();
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            return new StatusCodeResult(500);
        }
    }

    [FunctionName("AddArticle")]
    public static async Task<IActionResult> AddArticle(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "articles/add")] HttpRequest req, ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var newArticle = JsonConvert.DeserializeObject<Article>(requestBody);

        try {
            var container = CosmosClientManager.Instance.GetContainer("newshub", "articles");
            newArticle.Id = Guid.NewGuid().ToString();
            var response = await container.CreateItemAsync(newArticle, new PartitionKey(newArticle.Id));

            CacheManager.Invalidate();

            return new OkObjectResult(response.Resource);
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            return new StatusCodeResult(500);
        }
    }

    [FunctionName("UpdateArticle")]
    public static async Task<IActionResult> UpdateArticle([HttpTrigger(AuthorizationLevel.Function, "put", Route = "articles/update/{id}")] HttpRequest req, ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedArticle = JsonConvert.DeserializeObject<Article>(requestBody);

        try {
            var container = CosmosClientManager.Instance.GetContainer("newshub", "articles");
            var response = await container.ReplaceItemAsync(updatedArticle, updatedArticle.Id, new PartitionKey(updatedArticle.Id));

            CacheManager.Invalidate();

            return new OkObjectResult(response.Resource);
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            return new StatusCodeResult(500);
        }
    }

    [FunctionName("DeleteArticle")]
    public static async Task<IActionResult> DeleteArticle(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "articles/delete/{id}")] HttpRequest req, ILogger log, string id)
    {
        try
        {
            var container = CosmosClientManager.Instance.GetContainer("newshub", "articles");
            var response = await container.DeleteItemAsync<Article>(id, new PartitionKey(id));

            CacheManager.Invalidate();

            return new OkObjectResult(response.Resource);
        }
        catch (Exception e)
        {
            log.LogError(e.Message);
            return new StatusCodeResult(500);
        }
    }    
}

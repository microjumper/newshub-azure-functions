using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;
using Newtonsoft.Json;
using newshub.types;

namespace newshub.functions;

public static class HttpTrigger
{
    [FunctionName("GetArticle")]
    public static IActionResult GetArticle([HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/get/{id}")] HttpRequest req, ILogger log, string id)
    {
        CosmosClient cosmosClient = null;

        try 
        {
            Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

            Article article = GetArticleById(container, id);
        
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
        finally
        {
            cosmosClient?.Dispose();
        }
    }

    private static Article GetArticleById(Container container, string id)
    {
        return container.GetItemLinqQueryable<Article>(true).Where(a => a.Id == id).AsEnumerable().FirstOrDefault();
    } 

#region Add
    [FunctionName("AddArticle")]
    public static async Task<IActionResult> AddArticle(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "articles/add")] HttpRequest req,
        [CosmosDB(
            databaseName: "%DATABASE_NAME%",
            collectionName: "%COLLECTION_NAME%",
            ConnectionStringSetting = "CONNECTION_STRING_SETTING"
        )] IAsyncCollector<Article> articles, ILogger log)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var newArticle = JsonConvert.DeserializeObject<Article>(requestBody);
        await articles.AddAsync(newArticle);
        return new OkObjectResult(newArticle);
    }
#endregion

#region Update
    [FunctionName("UpdateArticle")]
    public static async Task<IActionResult> UpdateArticle([HttpTrigger(AuthorizationLevel.Function, "put", Route = "articles/update/{id}")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var updatedArticle = JsonConvert.DeserializeObject<Article>(requestBody);

        using CosmosClient cosmosClient = new(
            connectionString: Environment.GetEnvironmentVariable("CONNECTION_STRING_SETTING"),
            new CosmosClientOptions()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }
        );
        var container = cosmosClient.GetContainer("newshub", "articles");
        var response = await container.ReplaceItemAsync(updatedArticle, updatedArticle.Id, new PartitionKey(updatedArticle.Id));

        return new OkObjectResult(response.Resource);
    }
#endregion

#region Delete
    [FunctionName("DeleteArticle")]
    public static async Task<IActionResult> DeleteArticle(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "articles/delete/{id}")] HttpRequest req, string id)
    {
        using CosmosClient cosmosClient = new(
            connectionString: Environment.GetEnvironmentVariable("CONNECTION_STRING_SETTING"),
            new CosmosClientOptions()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            }
        );
        var container = cosmosClient.GetContainer("newshub", "articles");
        var response = await container.DeleteItemAsync<Article>(id, new PartitionKey(id));

        return new OkObjectResult(response.Resource);
    }
#endregion
    
}

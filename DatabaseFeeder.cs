using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using newshub.types;

namespace newshub.functions;

public static class DatabaseFeeder
{
    [FunctionName("FeedDatabase")]
    public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        var requestBody = req.ReadAsStringAsync().Result;
        var articles = JsonConvert.DeserializeObject<List<Article>>(requestBody);

        Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

        foreach (var article in articles)
        {
            article.Id = Guid.NewGuid().ToString();
            
            var response = await container.CreateItemAsync(article, new PartitionKey(article.Id));

            if (response.StatusCode != HttpStatusCode.Created)
            {
                log.LogError($"Failed to add item to Cosmos DB. StatusCode: {response.StatusCode}");

                return new BadRequestObjectResult("Failed to add item to Cosmos DB");
            }
        }

        return new OkObjectResult(articles);
    }
}

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using newshub.types;

namespace newshub.functions;

public static class Paginator
{
    [FunctionName("GetArticles")]
    public static async Task<IActionResult> GetArticles(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/get/all")] HttpRequest  req)
    {
        Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

        var query = new QueryDefinition("SELECT * FROM c");

        var response = await container.GetItemQueryIterator<Article>(query).ReadNextAsync();

        var articles = response.ToList();
        
        return new OkObjectResult(articles);
    }

    [FunctionName("Search")]
    public static async Task<IActionResult> Search(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "articles/search/{search:alpha}")] HttpRequest  req,
        string search)
    {
        Container container = CosmosClientManager.Instance.GetContainer("newshub", "articles");

        var query = new QueryDefinition("SELECT * FROM c WHERE CONTAINS(LOWER(c.title), LOWER(@searchTerm))")
            .WithParameter("@searchTerm", search);

        var response = await container.GetItemQueryIterator<Article>(query).ReadNextAsync();

        var articles = response.ToList();
        
        return new OkObjectResult(articles);
    }
}

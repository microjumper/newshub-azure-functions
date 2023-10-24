using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using newshub.functions.utils;

namespace newshub.functions;

public static class CosmosDBTrigger
{
    [FunctionName("CosmosDBTrigger")]
    public static void Run([CosmosDBTrigger(
        databaseName: "newshub",
        collectionName: "articles",
        ConnectionStringSetting = "CONNECTION_STRING_SETTING",
        LeaseCollectionName = "leases",
        CreateLeaseCollectionIfNotExists = true)]IReadOnlyList<Document> input,
        ILogger log)
    {
        if (input != null && input.Count > 0)
        {
            log.LogInformation("Articles modified " + input.Count);

            CacheManager.Invalidate();
        }
    }
}
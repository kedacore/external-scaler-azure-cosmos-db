using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Azure.Identity;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbFactory
    {
        // As per https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient, it is recommended to
        // maintain a single instance of CosmosClient per lifetime of the application.
        private readonly ConcurrentDictionary<string, CosmosClient> _cosmosClientCache = new();

        public CosmosClient GetCosmosClient(string endpoint)
        {
            return _cosmosClientCache.GetOrAdd(endpoint, CreateCosmosClient);
        }

        //private CosmosClient CreateCosmosClient(string connection)
        private CosmosClient CreateCosmosClient(string endpoint)
        {

            var credential = new DefaultAzureCredential();
            return new Microsoft.Azure.Cosmos.CosmosClient(endpoint, credential, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
           

        }
    }
}

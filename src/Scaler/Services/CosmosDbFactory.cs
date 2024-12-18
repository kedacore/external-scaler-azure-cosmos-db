using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbFactory
    {
        // As per https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient, it is recommended to
        // maintain a single instance of CosmosClient per lifetime of the application.
        private readonly ConcurrentDictionary<string, CosmosClient> _cosmosClientCache = new();

        public CosmosClient GetCosmosClient(string connectionFromEnv)
        {
            return _cosmosClientCache.GetOrAdd(connectionFromEnv, CreateCosmosClient);
        }

        private CosmosClient CreateCosmosClient(string connectionFromEnv)
        {
            return new CosmosClient(connectionFromEnv, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
        }
    }
}

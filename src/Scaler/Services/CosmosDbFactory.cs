using System.Collections.Concurrent;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbFactory
    {
        // As per https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient, it is recommended to
        // maintain a single instance of CosmosClient per lifetime of the application.
        private readonly ConcurrentDictionary<string, CosmosClient> _cosmosClientCache = new();

        public CosmosClient GetCosmosClient(string connection) =>
            _cosmosClientCache.GetOrAdd(connection, CreateCosmosClient);

        private CosmosClient CreateCosmosClient(string connection) =>
            connection.Contains("Accountkey") ?
                new CosmosClient(connection, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway }) :
                new CosmosClient(connection, new DefaultAzureCredential(), new CosmosClientOptions { ConnectionMode = ConnectionMode.Direct });

    }
}

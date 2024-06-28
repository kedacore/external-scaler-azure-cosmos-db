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

        public CosmosClient GetCosmosClient(string endpoint, bool useCredetials)
        {
            return _cosmosClientCache.GetOrAdd(endpoint, ep => CreateCosmosClient(ep, useCredetials));
        }

        //private CosmosClient CreateCosmosClient(string connection)
        private CosmosClient CreateCosmosClient(string endpoint_OR_connection, bool useCredentials)
        {
            //use connection string or credentials
            if (useCredentials)
            {
                var credential = new DefaultAzureCredential();
                return new Microsoft.Azure.Cosmos.CosmosClient(endpoint_OR_connection, credential, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, ApplicationName = "keda-external-azure-cosmos-db" });
            }
            else
            {
                return new Microsoft.Azure.Cosmos.CosmosClient(endpoint_OR_connection, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway, ApplicationName = "keda-external-azure-cosmos-db" });
            }


        }
    }
}

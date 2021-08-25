using System;
using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDbScaler
{
    internal sealed class CosmosClientFactory
    {
        // As per https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient, it is recommended to
        // maintain a single instance of CosmosClient per lifetime of the application.
        private readonly ConcurrentDictionary<string, CosmosClient> _cache = new ConcurrentDictionary<string, CosmosClient>();

        public CosmosClient GetCosmosClient(string connection)
        {
            return _cache.GetOrAdd(connection, GetCosmosClientInternal);
        }

        private CosmosClient GetCosmosClientInternal(string connection)
        {
            // TODO: Check if gateway mode is necessary: https://docs.microsoft.com/azure/cosmos-db/sql-sdk-connection-modes.
            return new CosmosClient(connection, new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
        }
    }
}
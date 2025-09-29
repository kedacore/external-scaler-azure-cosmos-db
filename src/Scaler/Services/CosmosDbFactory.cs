using System.Collections.Concurrent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbFactory
    {
        private const string _applicationName = "KEDA-External-Scaler";
        // As per https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.cosmosclient, it is recommended to
        // maintain a single instance of CosmosClient per lifetime of the application.
        private readonly ConcurrentDictionary<(string, string), CosmosClient> _cosmosClientCache = new();

        public CosmosClient GetCosmosClient(string endpointOrConnection, bool useCredentials, string clientId)
        {
            return _cosmosClientCache.GetOrAdd((endpointOrConnection, clientId), CreateCosmosClient(endpointOrConnection, useCredentials, clientId));
        }

        private CosmosClient CreateCosmosClient(string endpointOrConnection, bool useCredentials, string clientId)
        {
            if (useCredentials)
            {
                return new CosmosClient(
                    endpointOrConnection, 
                    GetChainedCredential(clientId), 
                    new CosmosClientOptions { 
                        ConnectionMode = ConnectionMode.Gateway,
                        ApplicationName = _applicationName, 
                        LimitToEndpoint = true
                    });
            }
            else
            {
                return new CosmosClient(
                    endpointOrConnection, 
                    new CosmosClientOptions { 
                        ConnectionMode = ConnectionMode.Gateway,
                        ApplicationName = _applicationName 
                    });
            }
        }

        /// <summary>
        /// Returns a chained token credential to be used for authentication. Supports managed identity, workload identity credentials
        /// on Azure, while az CLI credentials can be used for local testing.
        /// </summary>
        /// <param name="clientId">ClientId of the identity to be used. System identity is used if this is null. </param>
        /// <returns></returns>
        public static TokenCredential GetChainedCredential(string clientId)
        {
            return new ChainedTokenCredential(
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId = clientId,
                            ExcludeInteractiveBrowserCredential = true
                        }),
                new WorkloadIdentityCredential(
                    new WorkloadIdentityCredentialOptions 
                    { 
                        ClientId = clientId 
                    }),
                new AzureCliCredential());
        }
    }
}

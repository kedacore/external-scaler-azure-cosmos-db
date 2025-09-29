using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Keda.CosmosDb.Scaler.Demo.Shared
{
    public static class DemoHelper
    {
        /// <summary>
        /// Creates a CosmosClient either using connection string or managed identity based on the input parameters.
        /// </summary>
        /// <param name="endpointOrConnection">Endpoint or connection string for the Cosmos DB account.</param>
        /// <param name="useCredentials">Whether to use managed identity credentials.</param>
        /// <param name="clientId">Client ID for the managed identity.</param>
        /// <param name="applicationName">Name of the application.</param>
        /// <returns>A CosmosClient instance.</returns>
        public static CosmosClient CreateCosmosClient(string endpointOrConnection, bool useCredentials, string clientId, string applicationName)
        {
            if (useCredentials)
            {
                return new CosmosClient(
                    endpointOrConnection,
                    GetChainedCredential(clientId),
                    new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Gateway,
                        ApplicationName = applicationName,
                        LimitToEndpoint = true
                    });
            }
            else
            {
                return new CosmosClient(
                    endpointOrConnection,
                    new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Gateway,
                        ApplicationName = applicationName
                    });
            }
        }

        /// <summary>
        /// Returns a chained token credential to be used for authentication. Supports managed identity, workload identity credentials
        /// on Azure, while az CLI credentials can be used for local testing.
        /// </summary>
        /// <param name="clientId">Client ID for the managed identity.</param>
        /// <returns>A chained token credential.</returns>
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

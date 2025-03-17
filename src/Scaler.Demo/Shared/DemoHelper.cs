using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace Keda.CosmosDb.Scaler.Demo.Shared
{
    public static class DemoHelper
    {
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

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
                var credential = GetChainedCredential(clientId);
                return new CosmosClient(
                    endpointOrConnection,
                    credential,
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
            var options = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId,
                ExcludeInteractiveBrowserCredential = true
            };

            var workloadIdentityCredentials = new WorkloadIdentityCredential(new WorkloadIdentityCredentialOptions { ClientId = clientId });
            var azCliCredentials = new AzureCliCredential();

            return new ChainedTokenCredential(
                new DefaultAzureCredential(options),
                workloadIdentityCredentials,
                azCliCredentials);
        }
    }
}

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbMetricProvider : ICosmosDbMetricProvider
    {
        private readonly CosmosDbFactory _factory;
        private readonly ILogger<CosmosDbMetricProvider> _logger;

        public CosmosDbMetricProvider(CosmosDbFactory factory, ILogger<CosmosDbMetricProvider> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private CosmosClient GetCosmosClientFromMetadata(ScalerMetadata metadata, bool isLeaseContainer)
        {
            // Default to main container values
            string connectionString = metadata.Connection;
            string endpoint = metadata.Endpoint;

            // Override with lease container values if applicable
            if (isLeaseContainer)
            {
                connectionString = metadata.LeaseConnection ?? metadata.Connection;
                endpoint = metadata.LeaseEndpoint ?? metadata.Endpoint;
            }

            // Prioritize credential-based connections
            // Note: if ClientId is null, the Azure Workload Identity controller may inject client ID from service account annotations
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogTrace($"Using MSI credentials for CosmosClient with endpoint: [{endpoint}] and clientId: [{metadata.ClientId}] .");
                return _factory.GetCosmosClient(endpoint, useCredentials: true, clientId: metadata.ClientId);
            }
            else
            {
                _logger.LogTrace($"Using connection string for CosmosClient with Connection: [{connectionString}].");
                return _factory.GetCosmosClient(connectionString, useCredentials: false, clientId: null);
            }
        }

        public async Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata)
        {
            try
            {
                Container leaseContainer = GetCosmosClientFromMetadata(scalerMetadata, isLeaseContainer: true)
                    .GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);

                ChangeFeedEstimator estimator = GetCosmosClientFromMetadata(scalerMetadata, isLeaseContainer: false)
                    .GetContainer(scalerMetadata.DatabaseId, scalerMetadata.ContainerId)
                    .GetChangeFeedEstimator(scalerMetadata.ProcessorName, leaseContainer);

                // It does not help by creating more change-feed processing instances than the number of partitions.
                int partitionCount = 0;
                using (FeedIterator<ChangeFeedProcessorState> iterator = estimator.GetCurrentStateIterator())
                {
                    while (iterator.HasMoreResults)
                    {
                        FeedResponse<ChangeFeedProcessorState> states = await iterator.ReadNextAsync();
                        partitionCount += states.Where(state => state.EstimatedLag > 0).Count();
                    }
                }

                return partitionCount;
            }
            catch (CosmosException exception)
            {
                _logger.LogWarning($"Encountered exception {exception.GetType()}: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning($"Encountered exception {exception.GetType()}: {exception.Message}");
                throw;
            }
            catch (HttpRequestException exception)
            {
                var webException = exception.InnerException as WebException;
                if (webException?.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = (HttpWebResponse)webException.Response;
                    _logger.LogWarning($"Encountered error response {response.StatusCode}: {response.StatusDescription}");
                }
                else
                {
                    _logger.LogWarning($"Encountered exception {exception.GetType()}: {exception.Message}");
                }
            }

            return 0L;
        }
    }
}

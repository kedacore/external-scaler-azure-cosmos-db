using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
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

        public async Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata)
        {
            try
            {
                Container leaseContainer = _factory
                    .GetCosmosClient(scalerMetadata.LeaseConnection ?? scalerMetadata.LeaseEndpoint)
                    .GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);

                ChangeFeedEstimator estimator = _factory
                    .GetCosmosClient(scalerMetadata.Connection ?? scalerMetadata.Endpoint)
                    .GetContainer(scalerMetadata.DatabaseId, scalerMetadata.ContainerId)
                    .GetChangeFeedEstimator(scalerMetadata.ProcessorName, leaseContainer);

                // It does not help by creating more change-feed processing instances than the number of partitions.
                int partitionCount = 0;

                using (FeedIterator<ChangeFeedProcessorState> iterator = estimator.GetCurrentStateIterator())
                {
                    while (iterator.HasMoreResults)
                    {
                        FeedResponse<ChangeFeedProcessorState> states = await iterator.ReadNextAsync();

                        foreach (ChangeFeedProcessorState leaseState in states)
                        {
                            string host = leaseState.InstanceName == null ? $"not owned by any host currently" : $"owned by host {leaseState.InstanceName}";
                            _logger.LogInformation("Lease [{LeaseToken}] {host} reports {EstimatedLag} as estimated lag.", leaseState.LeaseToken, host, leaseState.EstimatedLag);

                            partitionCount += leaseState.EstimatedLag > 0 ? 1 : 0;
                        }
                    }
                }

                _logger.LogInformation("Returning active {partitionCount}", partitionCount);
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

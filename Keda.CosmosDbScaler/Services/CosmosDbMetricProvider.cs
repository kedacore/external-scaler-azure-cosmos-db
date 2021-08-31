using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDbScaler
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
                    .GetCosmosClient(scalerMetadata.LeaseConnection)
                    .GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);

                ChangeFeedEstimator estimator = _factory
                    .GetCosmosClient(scalerMetadata.Connection)
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

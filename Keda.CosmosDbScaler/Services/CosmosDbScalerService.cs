using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDbScaler
{
    internal sealed class CosmosDbScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly CosmosDbFactory _cosmosDbFactory;
        private readonly ILogger<CosmosDbScalerService> _logger;

        public CosmosDbScalerService(CosmosDbFactory cosmosDbFactory, ILogger<CosmosDbScalerService> logger)
        {
            _cosmosDbFactory = cosmosDbFactory ?? throw new ArgumentNullException(nameof(cosmosDbFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request);

            bool isActive = (await GetPartitionCountAsync(scalerMetadata)) > 0L;
            return new IsActiveResponse { Result = isActive };
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request.ScaledObjectRef);

            var response = new GetMetricsResponse();

            response.MetricValues.Add(new MetricValue
            {
                MetricName = scalerMetadata.MetricName,
                MetricValue_ = await GetPartitionCountAsync(scalerMetadata),
            });

            return response;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request);

            var response = new GetMetricSpecResponse();

            response.MetricSpecs.Add(new MetricSpec
            {
                MetricName = scalerMetadata.MetricName,
                TargetSize = 1L,
            });

            return Task.FromResult(response);
        }

        private async Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata)
        {
            try
            {
                Container leaseContainer = _cosmosDbFactory
                    .GetCosmosClient(scalerMetadata.LeaseConnection)
                    .GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);

                ChangeFeedEstimator estimator = _cosmosDbFactory
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

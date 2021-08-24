using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDbScaler
{
    public class CosmosDbScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly ILogger<CosmosDbScalerService> _logger;

        public CosmosDbScalerService(ILogger<CosmosDbScalerService> logger)
        {
            _logger = logger;
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            bool isActive = (await GetPartitionCountAsync(request)) > 0L;
            return new IsActiveResponse { Result = isActive };
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var response = new GetMetricsResponse();

            response.MetricValues.Add(new MetricValue
            {
                MetricName = "cosmos-partition-count",
                MetricValue_ = await GetPartitionCountAsync(request.ScaledObjectRef),
            });

            return response;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var response = new GetMetricSpecResponse();

            response.MetricSpecs.Add(new MetricSpec
            {
                MetricName = "cosmos-partition-count", // TODO: Check name and uniqueness requirements.
                TargetSize = 1L,
            });

            return Task.FromResult(response);
        }

        private async Task<long> GetPartitionCountAsync(ScaledObjectRef scaledObjectRef)
        {
            var scalerMetadata = ScalerMetadata.Create(scaledObjectRef);

            // TODO: Check if gateway mode is necessary: https://docs.microsoft.com/en-us/azure/cosmos-db/sql-sdk-connection-modes.
            CosmosClient monitoredClient = new CosmosClient(
                scalerMetadata.Connection,
                new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });

            CosmosClient leaseClient;
            if (scalerMetadata.LeaseConnection == scalerMetadata.Connection)
            {
                leaseClient = monitoredClient;
            }
            else
            {
                leaseClient = new CosmosClient(
                    scalerMetadata.LeaseConnection,
                    new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
            }

            Container monitoredContainer = monitoredClient.GetContainer(scalerMetadata.DatabaseId, scalerMetadata.ContainerId);
            Container leaseContainer = leaseClient.GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);

            // TODO: Check behavior when an exception is thrown.
            try
            {
                ChangeFeedEstimator estimator = monitoredContainer.GetChangeFeedEstimator(scalerMetadata.ProcessorName, leaseContainer);

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

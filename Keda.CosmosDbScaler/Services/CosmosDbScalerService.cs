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
            bool isActive = (await GetRemainingWorkAsync(request.ScalerMetadata)) > 0L;
            return new IsActiveResponse { Result = isActive };
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var response = new GetMetricsResponse();

            response.MetricValues.Add(new MetricValue
            {
                MetricName = "cosmos-remaining-work",
                MetricValue_ = await GetRemainingWorkAsync(request.ScaledObjectRef.ScalerMetadata),
            });

            return response;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var response = new GetMetricSpecResponse();

            response.MetricSpecs.Add(new MetricSpec
            {
                MetricName = "cosmos-remaining-work", // TODO: Check name and uniqueness requirements.
                TargetSize = 1L, // TODO: Understand why this should equal 1 (it seems to be 1000 in WebJobs SDK).
            });

            return Task.FromResult(response);
        }

        private async Task<long> GetRemainingWorkAsync(MapField<string, string> scalerMetadata)
        {
            _logger.LogInformation($"TEST: Creating monitored client: {scalerMetadata["connection"]}");

            // TODO: Check if gateway mode is necessary: https://docs.microsoft.com/en-us/azure/cosmos-db/sql-sdk-connection-modes.
            CosmosClient monitoredClient = new CosmosClient(
                scalerMetadata["connection"],
                new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });

            CosmosClient leaseClient;
            if (scalerMetadata["leaseConnection"].Equals(scalerMetadata["connection"], StringComparison.OrdinalIgnoreCase))
            {
                leaseClient = monitoredClient;
            }
            else
            {
                _logger.LogInformation($"TEST: Creating lease client: {scalerMetadata["connection"]}");

                leaseClient = new CosmosClient(
                    scalerMetadata["leaseConnection"],
                    new CosmosClientOptions { ConnectionMode = ConnectionMode.Gateway });
            }

            Container monitoredContainer = monitoredClient.GetContainer(scalerMetadata["databaseId"], scalerMetadata["containerId"]);
            Container leaseContainer = leaseClient.GetContainer(scalerMetadata["leaseDatabaseId"], scalerMetadata["leaseContainerId"]);

            // TODO: Check if estimator instances need caching.
            // TODO: Check behavior when an exception is thrown.
            // TODO: Check if logging works.
            try
            {
                _logger.LogInformation($"TEST: Creating estimator: {scalerMetadata["processorName"]}");

                List<ChangeFeedProcessorState> partitionWorkList = new List<ChangeFeedProcessorState>();
                ChangeFeedEstimator estimator = monitoredContainer.GetChangeFeedEstimator(scalerMetadata["processorName"], leaseContainer);

                using (FeedIterator<ChangeFeedProcessorState> iterator = estimator.GetCurrentStateIterator())
                {
                    while (iterator.HasMoreResults)
                    {
                        FeedResponse<ChangeFeedProcessorState> states = await iterator.ReadNextAsync();
                        partitionWorkList.AddRange(states);
                    }
                }

                _logger.LogInformation($"TEST: Partition count: {partitionWorkList.Count}");
                _logger.LogInformation($"TEST: Estimated items: {partitionWorkList.Sum(item => item.EstimatedLag)}");

                // Return sum of "approximations of the difference between the last processed item in the feed container
                // and the latest change recorded" across all processor instances.
                return partitionWorkList.Sum(item => item.EstimatedLag);
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

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Microsoft.Identity.Client;
namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbMetricProvider : ICosmosDbMetricProvider
    {
        private readonly CosmosDbFactory _factory;
        private readonly ILogger<CosmosDbMetricProvider> _logger;

		static Meter s_meter = new Meter("OrderProcessor.CFStore", "1.0.0");
        private static Counter<long> s_CFRecordsReceived = s_meter.CreateCounter<long>("RecordsReceived");
        private static Counter<int> s_CFProcessorCount = s_meter.CreateCounter<int>("ProcessorCount");
        public CosmosDbMetricProvider(CosmosDbFactory factory, ILogger<CosmosDbMetricProvider> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
               .AddMeter("OrderProcessor.CFStore")               
               .Build();
        }

        public async Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata)
        {
            try
            {

                var credential = new DefaultAzureCredential();
                var cosmosClient = new CosmosClient(scalerMetadata.Endpoint, credential);


                Container leaseContainer = cosmosClient.GetContainer(scalerMetadata.LeaseDatabaseId, scalerMetadata.LeaseContainerId);
                Container monitoredContainer = cosmosClient.GetContainer(scalerMetadata.DatabaseId, scalerMetadata.ContainerId);


                ChangeFeedEstimator changeFeedEstimator = monitoredContainer.GetChangeFeedEstimator("SalesOrderChangeFeed", leaseContainer);

                using FeedIterator<ChangeFeedProcessorState> estimatorIterator = changeFeedEstimator.GetCurrentStateIterator();
                int partitionCount = 0;

                long lagCount=0;
                while (estimatorIterator.HasMoreResults)
                {
                    FeedResponse<ChangeFeedProcessorState> states = await estimatorIterator.ReadNextAsync();
                   
                    foreach (ChangeFeedProcessorState leaseState in states)
                    {
                        string host = leaseState.InstanceName == null ? $"not owned by any host currently" : $"owned by host {leaseState.InstanceName}";
                        _logger.LogInformation($"Lease [{leaseState.LeaseToken}] {host} reports {leaseState.EstimatedLag} as estimated lag.");
                        lagCount = lagCount + leaseState.EstimatedLag;

                    }
                    partitionCount += states.Where(state => state.EstimatedLag > 0).Count();
                }

                s_CFRecordsReceived.Add(lagCount);
                s_CFProcessorCount.Add(partitionCount);
                _logger.LogInformation($"Count of Partitions with lag:{partitionCount}");

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

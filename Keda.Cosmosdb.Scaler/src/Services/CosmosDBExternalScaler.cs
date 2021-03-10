using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Keda.CosmosDB.Scaler.Protos;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using static Keda.CosmosDB.Scaler.Protos.ExternalScaler;

namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBExternalScaler : ExternalScalerBase
    {
        private readonly ILogger _logger;

        public CosmosDBExternalScaler(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CosmosDBExternalScaler>();
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            try
            {
                var trigger = CreateTriggerFromMetadata(request.ScalerMetadata);
                var workToBeDone = await GetEstimatedWork(trigger);

                bool isActive = workToBeDone > 0;

                if (isActive)
                {
                    _logger.LogDebug(string.Format("Activating to 1 for cosmosDB account {0} database {1} collection {2}", trigger.AccountName, trigger.DatabaseName, trigger.CollectionName));
                }

                return new IsActiveResponse
                {
                    Result = isActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting estimated work.");
                throw ex;
            }
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var resp = new GetMetricSpecResponse();
            resp.MetricSpecs.Add(new MetricSpec
            {
                MetricName = NormalizeString(string.Format("{0}-{1}-{2}-{3}", "azure-cosmosDB",
                                        request.ScalerMetadata[CosmosDBTriggerMetadata.AccountName],
                                        request.ScalerMetadata[CosmosDBTriggerMetadata.DatabaseName],
                                        request.ScalerMetadata[CosmosDBTriggerMetadata.CollectionName])),
                TargetSize = 1
            });
            return Task.FromResult(resp);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var resp = new GetMetricsResponse();
            try
            {
                var trigger = CreateTriggerFromMetadata(request.ScaledObjectRef.ScalerMetadata);
                long workToBeDone = await GetEstimatedWork(trigger);

                var metricName = request.ScaledObjectRef.ScalerMetadata["MetricName"];
                resp.MetricValues.Add(new MetricValue
                {
                    MetricName = metricName,
                    MetricValue_ = workToBeDone
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating GetMetricsResponse");
                throw ex;
            }
            return resp;
        }

        public CosmosDBTrigger CreateTriggerFromMetadata(MapField<string, string> scalerMetadata)
        {
            var lease = new CosmosDBLease()
            {
                LeasesCosmosDBConnectionString = scalerMetadata[CosmosDBLeaseMetadata.LeasesCosmosDBConnectionString],
                LeaseDatabaseName = scalerMetadata[CosmosDBLeaseMetadata.LeaseDatabaseName],
                LeaseCollectionName = scalerMetadata[CosmosDBLeaseMetadata.LeaseCollectionName]
            };

            var trigger = new CosmosDBTrigger()
            {
                CollectionName = scalerMetadata[CosmosDBTriggerMetadata.CollectionName],
                DatabaseName = scalerMetadata[CosmosDBTriggerMetadata.DatabaseName],
                CosmosDBConnectionString = scalerMetadata[CosmosDBTriggerMetadata.CosmosDBConnectionString],
                AccountName = string.Empty,
                Lease = lease
            };

            if (scalerMetadata.TryGetValue(CosmosDBTriggerMetadata.AccountName, out string accountName))
            {
                trigger.AccountName = accountName;
            }

            return trigger;
        }

        private async Task<long> GetEstimatedWork (CosmosDBTrigger trigger)
        {
            try
            {
                var builder = CreateChangeFeedProcessorBuilder(trigger);
                var workEstimator = await builder.BuildEstimatorAsync();
                long estimatedRemainingWork = await workEstimator.GetEstimatedRemainingWork();
                return estimatedRemainingWork;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting estimated work from host");
                throw ex;
            }
        }

        private static ChangeFeedProcessorBuilder CreateChangeFeedProcessorBuilder(
            CosmosDBTrigger trigger)
        {
            CosmosDBConnectionString triggerConnection = new CosmosDBConnectionString(trigger.CosmosDBConnectionString);
            DocumentCollectionInfo documentCollectionLocation = new DocumentCollectionInfo
            {
                Uri = triggerConnection.ServiceEndpoint,
                MasterKey = triggerConnection.AuthKey,
                DatabaseName = trigger.DatabaseName,
                CollectionName = trigger.CollectionName
            };

            CosmosDBLease lease = trigger.Lease;
            CosmosDBConnectionString leaseConnection = new CosmosDBConnectionString(lease.LeasesCosmosDBConnectionString);

            DocumentCollectionInfo leaseCollectionLocation = new DocumentCollectionInfo
            {
                Uri = leaseConnection.ServiceEndpoint,
                MasterKey = leaseConnection.AuthKey,
                DatabaseName = lease.LeaseDatabaseName,
                CollectionName = lease.LeaseCollectionName
            };

            HostPropertiesCollection hostProperties = new HostPropertiesCollection(documentCollectionLocation, leaseCollectionLocation);

            var changeFeedClient = new DocumentClient(documentCollectionLocation.Uri, documentCollectionLocation.MasterKey);
            IChangeFeedDocumentClient feedDocumentClient = new ChangeFeedDocumentClient(changeFeedClient);

            var leaseClient = new DocumentClient(leaseCollectionLocation.Uri, leaseCollectionLocation.MasterKey);
            IChangeFeedDocumentClient leaseDocumentClient = new ChangeFeedDocumentClient(leaseClient);

            var builder = new ChangeFeedProcessorBuilder()
                            .WithHostName(hostProperties.HostName)
                            .WithFeedCollection(documentCollectionLocation)
                            .WithLeaseCollection(leaseCollectionLocation)
                            .WithFeedDocumentClient(feedDocumentClient)
                            .WithLeaseDocumentClient(leaseDocumentClient);
            return builder;
        }

        private static string NormalizeString(string inputString)
        {
            return inputString.Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-");
        }
    }
}
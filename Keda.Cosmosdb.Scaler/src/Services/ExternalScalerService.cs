using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Keda.Cosmosdb.Scaler.Protos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.Client;

namespace Keda.Cosmosdb.Scaler.Services
{
    public class ExternalScalerService : ExternalScaler.ExternalScalerBase
    {
        private static ConsoleLogger logger = new ConsoleLogger();
        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            bool isActive = false;
            try
            {
                var trigger = CreateTriggerFromMetadata(request.ScalerMetadata);
                var workToBeDone = await GetEstimatedWork(trigger);

                isActive = workToBeDone > 0;

                if (isActive)
                {
                    logger.Log(string.Format("Activating to 1 for cosmosDB account {0} database {1} collection {2}", trigger.AccountName, trigger.DatabaseName, trigger.CollectionName));
                }
            }
            catch(Exception ex)
            {
                logger.Log(ex.Message);
            }

            return new IsActiveResponse
            {
                Result = isActive
            };
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var resp = new GetMetricSpecResponse();
            resp.MetricSpecs.Add(new MetricSpec
            {
                MetricName = NormalizeString(string.Format("{0}-{1}-{2}-{3}", "azure-cosmosDB",
                                        request.ScalerMetadata[CosmosDbTriggerMetadata.AccountName],
                                        request.ScalerMetadata[CosmosDbTriggerMetadata.DatabaseName],
                                        request.ScalerMetadata[CosmosDbTriggerMetadata.CollectionName])),
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
                logger.Log(ex.Message);
            }
            return resp;
        }

        private CosmosDbTrigger CreateTriggerFromMetadata(MapField<string, string> scalerMetadata)
        {
            var trigger = new CosmosDbTrigger()
            {
                CollectionName = scalerMetadata[CosmosDbTriggerMetadata.CollectionName],
                DatabaseName = scalerMetadata[CosmosDbTriggerMetadata.DatabaseName],
                DocDBConnectionString = scalerMetadata[CosmosDbTriggerMetadata.DocDBConnectionString],
                LeasesDocDBConnectionString = scalerMetadata[CosmosDbTriggerMetadata.LeasesDocDBConnectionString],
                LeaseDatabaseName = scalerMetadata[CosmosDbTriggerMetadata.LeaseDatabaseName],
                LeaseCollectionName = scalerMetadata[CosmosDbTriggerMetadata.LeaseCollectionName],
                LeaseCollectionPrefix = string.Empty,
                AccountName = string.Empty,
            };

            // Optional values
            if (scalerMetadata.TryGetValue(CosmosDbTriggerMetadata.LeaseCollectionPrefix, out string leasePrefix))
            {
                trigger.LeaseCollectionPrefix = leasePrefix;
            }

            if (scalerMetadata.TryGetValue(CosmosDbTriggerMetadata.AccountName, out string accountName))
            {
                trigger.AccountName = accountName;
            }

            return trigger;
        }

        private async Task<long> GetEstimatedWork (CosmosDbTrigger trigger)
        {
            var host = CreateHost(trigger, out string warningMessage);
            long workToBeDone = 0;

            if (host != null)
            {
                workToBeDone = await host.GetEstimatedRemainingWork();
            }
            else
            {
                logger.Log(warningMessage);
            }

            return workToBeDone;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        private static ChangeFeedEventHost CreateHost(
#pragma warning restore CS0618 // Type or member is obsolete
            CosmosDbTrigger trigger,
            out string warningMessage)
        {
            DocumentDBConnectionString triggerConnection = new DocumentDBConnectionString(trigger.DocDBConnectionString);
            DocumentDBConnectionString leaseConnection = new DocumentDBConnectionString(trigger.LeasesDocDBConnectionString);

            DocumentCollectionInfo documentCollectionLocation = new DocumentCollectionInfo
            {
                Uri = triggerConnection.ServiceEndpoint,
                MasterKey = triggerConnection.AuthKey,
                DatabaseName = trigger.DatabaseName,
                CollectionName = trigger.CollectionName
            };

            DocumentCollectionInfo leaseCollectionLocation = new DocumentCollectionInfo
            {
                Uri = leaseConnection.ServiceEndpoint,
                MasterKey = leaseConnection.AuthKey,
                DatabaseName = trigger.LeaseDatabaseName,
                CollectionName = trigger.LeaseCollectionName
            };

            HostPropertiesCollection hostProperties = new HostPropertiesCollection(documentCollectionLocation, leaseCollectionLocation);

            ChangeFeedEventHost host = null;
            warningMessage = string.Empty;

            try
            {
                ChangeFeedHostOptions changeFeedHostOptions = new ChangeFeedHostOptions();
                if (!string.IsNullOrEmpty(trigger.LeaseCollectionPrefix))
                {
                    changeFeedHostOptions.LeasePrefix = trigger.LeaseCollectionPrefix;
                }

                host = new ChangeFeedEventHost(hostProperties.HostName, hostProperties.DocumentCollectionLocation, hostProperties.LeaseCollectionLocation, new ChangeFeedOptions(), changeFeedHostOptions);
            }
            catch (Exception e)
            {
                warningMessage = string.Format("Unable to create the ChangeFeedEventHost. Exception: {0}", e.Message);
            }

            return host;
        }

        private static string NormalizeString(string inputString)
        {
            return inputString.Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-");
        }
    }
}
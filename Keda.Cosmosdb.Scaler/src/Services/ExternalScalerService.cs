using System;
using System.Data.Common;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Keda.Cosmosdb.Scaler.Protos;
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
                    logger.Log(string.Format("Activating to 1 for cosmosDB database {0} collection {1}", trigger.DatabaseName, trigger.CollectionName));
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
                MetricName = "WorkToBeDone",
                TargetSize = 1
            });
            return Task.FromResult(resp);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            long workToBeDone = 0;
            try
            {
                var trigger = CreateTriggerFromMetadata(request.ScaledObjectRef.ScalerMetadata);
                workToBeDone = await GetEstimatedWork(trigger);
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
            }

            var resp = new GetMetricsResponse();
            resp.MetricValues.Add(new MetricValue
            {
                MetricName = "WorkToBeDone",
                MetricValue_ = workToBeDone
            });

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
                LeaseCollectionPrefix = string.Empty
            };

            // Optional values
            if (scalerMetadata.TryGetValue(CosmosDbTriggerMetadata.LeaseCollectionPrefix, out string leasePrefix))
            {
                trigger.LeaseCollectionPrefix = leasePrefix;
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

                host = new ChangeFeedEventHost(string.Empty, hostProperties.DocumentCollectionLocation, hostProperties.LeaseCollectionLocation, new ChangeFeedOptions(), changeFeedHostOptions);
            }
            catch (Exception e)
            {
                warningMessage = string.Format("Unable to create the ChangeFeedEventHost. Exception: {0}", e.Message);
            }

            return host;
        }

        internal class HostPropertiesCollection
        {
            public DocumentCollectionInfo DocumentCollectionLocation { get; private set; }

            public DocumentCollectionInfo LeaseCollectionLocation { get; private set; }

            public HostPropertiesCollection(DocumentCollectionInfo documentCollectionLocation, DocumentCollectionInfo leaseCollectionLocation)
            {
                this.DocumentCollectionLocation = documentCollectionLocation;
                this.LeaseCollectionLocation = leaseCollectionLocation;
            }
        }

        internal class DocumentDBConnectionString
        {
            public DocumentDBConnectionString(string connectionString)
            {
                // Use this generic builder to parse the connection string
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                object key = null;
                if (builder.TryGetValue("AccountKey", out key))
                {
                    AuthKey = key.ToString();
                }

                object uri;
                if (builder.TryGetValue("AccountEndpoint", out uri))
                {
                    ServiceEndpoint = new Uri(uri.ToString());
                }
            }

            public Uri ServiceEndpoint { get; set; }
            public string AuthKey { get; set; }
        }
    }
}
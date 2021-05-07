﻿using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Grpc.Core;
using Keda.Cosmosdb.Scaler;
using Keda.CosmosDB.Scaler.Protos;
using Microsoft.Extensions.Logging;
using static Keda.CosmosDB.Scaler.Protos.ExternalScaler;
using Keda.CosmosDB.Scaler.Extensions;
using Keda.CosmosDB.Scaler.Repository;
using Keda.Cosmosdb.Scaler.Repository;

namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBExternalScaler : ExternalScalerBase
    {
        private readonly ILogger _logger;
        private readonly ICosmosDBRepository _cosmosDBRepository;

        public CosmosDBExternalScaler(ILoggerFactory loggerFactory, ICosmosDBRepository cosmosDBRepository)
        {
            _logger = loggerFactory.CreateLogger<CosmosDBExternalScaler>();
            _cosmosDBRepository = cosmosDBRepository;
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            var trigger = CreateTriggerFromMetadata(request.ScalerMetadata);
            var workToBeDone = await GetEstimatedWork(trigger);

            bool isActive = workToBeDone > 0;

            if (isActive)
            {
                _logger.LogDebug("Activating to 1 instance for Azure Cosmos DB account {accountName} with database {databaseName} and collection {collectionName}",
                    trigger.AccountName, trigger.DatabaseName, trigger.CollectionName);
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
                MetricName = StringExtensions.NormalizeString(string.Format("{0}-{1}-{2}-{3}", Constants.AzureCosmosDBMetricPrefix,
                                        request.ScalerMetadata[Constants.AccountNameMetadata],
                                        request.ScalerMetadata[Constants.DatabaseNameMetadata],
                                        request.ScalerMetadata[Constants.CollectionNameMetadata])),
                TargetSize = 1
            });
            return Task.FromResult(resp);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var trigger = CreateTriggerFromMetadata(request.ScaledObjectRef.ScalerMetadata);
            long workToBeDone = await GetEstimatedWork(trigger);

            var metricName = request.ScaledObjectRef.ScalerMetadata["MetricName"];

            var resp = new GetMetricsResponse();
            resp.MetricValues.Add(new MetricValue
            {
                MetricName = metricName,
                MetricValue_ = workToBeDone
            });
            return resp;
        }

        public CosmosDBTrigger CreateTriggerFromMetadata(MapField<string, string> scalerMetadata)
        {
            var trigger = new CosmosDBTrigger(scalerMetadata[Constants.ConnectionStringMetadata],
                scalerMetadata[Constants.DatabaseNameMetadata], scalerMetadata[Constants.CollectionNameMetadata], string.Empty);

            trigger.Lease = new CosmosDBLease()
            {
                LeasesCosmosDBConnectionString = scalerMetadata[Constants.LeasesConnectionStringMetadata],
                LeaseDatabaseName = scalerMetadata[Constants.LeaseDatabaseNameMetadata],
                LeaseCollectionName = scalerMetadata[Constants.LeaseCollectionNameMetadata]
            };

            if (scalerMetadata.TryGetValue(Constants.AccountNameMetadata, out string accountName))
            {
                trigger.AccountName = accountName;
            }

            if (scalerMetadata.TryGetValue(Constants.LeaseCollectionPrefixMetadata, out string leasePrefix))
            {
                trigger.Lease.LeaseCollectionPrefix = leasePrefix;
            }

            return trigger;
        }

        private async Task<long> GetEstimatedWork(CosmosDBTrigger trigger)
        {
            var estimator = ChangeFeedEstimatorFactory.Instance.GetOrCreateEstimator(trigger);
            return await _cosmosDBRepository.GetEstimatedWork(estimator);
        }
    }
}
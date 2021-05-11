using Keda.CosmosDB.Scaler.Services;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public class CosmosDBEstimator : ICosmosDBEstimator
    {
        private ConcurrentDictionary<CosmosDBTrigger, ChangeFeedEstimator> _changeFeedBuilderMap;

        public CosmosDBEstimator()
        {
            _changeFeedBuilderMap = new ConcurrentDictionary<CosmosDBTrigger, ChangeFeedEstimator>(new CosmosDBTriggerComparer());
        }

        internal ChangeFeedEstimator GetOrCreateEstimator(CosmosDBTrigger trigger)
        {
            if (_changeFeedBuilderMap.TryGetValue(trigger, out ChangeFeedEstimator estimator))
            {
                return estimator;
            }

            CosmosClientOptions clientOptions = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Gateway
            };

            CosmosClient monitoredCosmosDBService = new CosmosClient(trigger.CosmosDBConnectionString, clientOptions);
            CosmosClient leaseCosmosDBService;

            if (trigger.CosmosDBConnectionString.Equals(
                trigger.Lease.LeasesCosmosDBConnectionString, StringComparison.OrdinalIgnoreCase))
            {
                leaseCosmosDBService = monitoredCosmosDBService;
            }
            else
            {
                leaseCosmosDBService = new CosmosClient(trigger.Lease.LeasesCosmosDBConnectionString, clientOptions);
            }

            var monitoredContainer = monitoredCosmosDBService.GetContainer(trigger.DatabaseName, trigger.CollectionName);
            var leaseContainer = leaseCosmosDBService.GetContainer(trigger.Lease.LeaseDatabaseName, trigger.Lease.LeaseCollectionName);

            estimator = monitoredContainer.GetChangeFeedEstimator(trigger.Lease.LeaseCollectionPrefix ?? string.Empty, leaseContainer);

            _changeFeedBuilderMap.TryAdd(trigger, estimator);
            return estimator;
        }

        public async Task<long> GetEstimatedWork(CosmosDBTrigger trigger)
        {
            ChangeFeedEstimator estimator = GetOrCreateEstimator(trigger);
            List<ChangeFeedProcessorState> partitionWorkList = new List<ChangeFeedProcessorState>();

            using FeedIterator<ChangeFeedProcessorState> estimatorIterator = estimator.GetCurrentStateIterator();
            {
                while (estimatorIterator.HasMoreResults)
                {
                    FeedResponse<ChangeFeedProcessorState> response = await estimatorIterator.ReadNextAsync();
                    partitionWorkList.AddRange(response);
                }
            }
            return partitionWorkList.Sum(item => item.EstimatedLag); ;
        }
    }
}

using Keda.CosmosDB.Scaler.Services;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;

namespace Keda.Cosmosdb.Scaler.Repository
{
    public class ChangeFeedEstimatorFactory
    {
        private Dictionary<CosmosDBTrigger, ChangeFeedEstimator> _changeFeedBuilderMap;
        private static ChangeFeedEstimatorFactory instance = new ChangeFeedEstimatorFactory();

        private ChangeFeedEstimatorFactory()
        {
            _changeFeedBuilderMap = new Dictionary<CosmosDBTrigger, ChangeFeedEstimator>(new CosmosDBTriggerComparer());
        }

        public static ChangeFeedEstimatorFactory Instance
        {
            get
            {
                return instance;
            }
        }

        public ChangeFeedEstimator GetOrCreateEstimator(CosmosDBTrigger trigger)
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
            CosmosClient leaseCosmosDBService = new CosmosClient(trigger.Lease.LeasesCosmosDBConnectionString, clientOptions);

            var monitoredContainer = monitoredCosmosDBService.GetContainer(trigger.DatabaseName, trigger.CollectionName);
            var leaseContainer = leaseCosmosDBService.GetContainer(trigger.Lease.LeaseDatabaseName, trigger.Lease.LeaseCollectionName);

            estimator = monitoredContainer.GetChangeFeedEstimator(trigger.Lease.LeaseCollectionPrefix ?? string.Empty, leaseContainer);

            _changeFeedBuilderMap.Add(trigger, estimator);
            return estimator;
        }
    }
}

﻿namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBLease
    {
        public string LeasesCosmosDBConnectionString { get; internal set; }
        public string LeaseDatabaseName { get; internal set; }
        public string LeaseCollectionName { get; internal set; }
        public string LeaseCollectionPrefix { get; internal set; }

        public CosmosDBLease()
        {
        }

        public CosmosDBLease(string leaseConnectionString, string leaseDatabaseName, string leaseCollectionName, string leaseCollectionPrefix)
        {
            LeasesCosmosDBConnectionString = leaseConnectionString;
            LeaseDatabaseName = leaseDatabaseName;
            LeaseCollectionName = leaseCollectionName;
            LeaseCollectionPrefix = leaseCollectionPrefix;
        }
    }
}
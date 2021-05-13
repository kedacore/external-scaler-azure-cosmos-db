using System;

namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBLease
    {
        public string LeasesCosmosDBConnectionString { get; internal set; }
        public string LeaseDatabaseName { get; internal set; }
        public string LeaseCollectionName { get; internal set; }
        public string LeaseCollectionPrefix { get; internal set; }

        public CosmosDBLease(string leaseConnectionString, string leaseDatabaseName, string leaseCollectionName)
        {
            LeasesCosmosDBConnectionString = leaseConnectionString ?? throw new ArgumentNullException(nameof(leaseConnectionString));
            LeaseDatabaseName = leaseDatabaseName ?? throw new ArgumentNullException(nameof(leaseDatabaseName));
            LeaseCollectionName = leaseCollectionName ?? throw new ArgumentNullException(nameof(LeaseCollectionName));
        }
    }
}

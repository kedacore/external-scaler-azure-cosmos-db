using Keda.CosmosDB.Scaler.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Keda.CosmosDB.Scaler.Repository
{
    public class CosmosDBTriggerComparer : IEqualityComparer<CosmosDBTrigger>
    {
        public bool Equals([AllowNull] CosmosDBTrigger x, [AllowNull] CosmosDBTrigger y)
        {
            return (x.AccountName.Equals(y.AccountName, StringComparison.OrdinalIgnoreCase) &&
                x.CollectionName.Equals(y.CollectionName, StringComparison.OrdinalIgnoreCase) &&
                x.CosmosDBConnectionString.Equals(y.CosmosDBConnectionString, StringComparison.OrdinalIgnoreCase) &&
                x.DatabaseName.Equals(y.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                x.Lease.LeasesCosmosDBConnectionString.Equals(y.Lease.LeasesCosmosDBConnectionString, StringComparison.OrdinalIgnoreCase) &&
                x.Lease.LeaseDatabaseName.Equals(y.Lease.LeaseDatabaseName, StringComparison.OrdinalIgnoreCase) &&
                x.Lease.LeaseCollectionName.Equals(y.Lease.LeaseCollectionName, StringComparison.OrdinalIgnoreCase));
        }

        public int GetHashCode([DisallowNull] CosmosDBTrigger obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return obj.AccountName.GetHashCode() ^ obj.CollectionName.GetHashCode() ^ obj.CosmosDBConnectionString.GetHashCode()
                ^ obj.DatabaseName.GetHashCode() ^ obj.Lease.LeasesCosmosDBConnectionString.GetHashCode() ^ obj.Lease.LeaseDatabaseName.GetHashCode() ^ obj.Lease.LeaseCollectionName.GetHashCode();
        }
    }
}

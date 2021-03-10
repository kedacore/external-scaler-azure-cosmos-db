namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBLeaseMetadata
    {
        public static string LeasesCosmosDBConnectionString { get { return "leasesCosmosDBConnectionString"; } }
        public static string LeaseDatabaseName { get { return "leaseDatabaseName"; } }
        public static string LeaseCollectionName { get { return "leaseCollectionName"; } }
        public static string LeaseCollectionPrefix { get { return "leaseCollectionPrefix"; } }
    }
}

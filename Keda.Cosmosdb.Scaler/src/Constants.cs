namespace Keda.Cosmosdb.Scaler
{
    public class Constants
    {
        public const string AccountKey = "AccountKey";
        public const string AccountEndpoint = "AccountEndpoint";
        public const string AzureCosmosDBMetricPrefix = "azure-cosmosDB";
        public const string DefaultHostName = "defaultName";

        public const string CollectionNameMetadata = "collectionName";
        public const string DatabaseNameMetadata = "databaseName";
        public const string ConnectionStringMetadata ="cosmosDBConnectionString";
        public const string AccountNameMetadata = "accountName";

        public const string LeasesConnectionStringMetadata = "leasesCosmosDBConnectionString";
        public const string LeaseDatabaseNameMetadata = "leaseDatabaseName";
        public const string LeaseCollectionNameMetadata = "leaseCollectionName";
        public const string LeaseCollectionPrefixMetadata = "leaseCollectionPrefix";

        public const int GrpcPort = 4050;
    }
}

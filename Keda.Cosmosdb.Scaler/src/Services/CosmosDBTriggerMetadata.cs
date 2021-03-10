namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBTriggerMetadata
    {
        public static string CollectionName { get { return "collectionName"; } }
        public static string DatabaseName { get { return "databaseName"; } }
        public static string CosmosDBConnectionString { get { return "docDBConnectionString"; } }
        public static string AccountName { get { return "accountName"; } }
    }
}
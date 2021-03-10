namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBTrigger
    {
        public string CosmosDBConnectionString { get; internal set; }
        public string DatabaseName { get; internal set; }
        public string CollectionName { get; internal set; }
        public string AccountName { get; internal set; }
        public CosmosDBLease Lease { get; internal set; }

        public CosmosDBTrigger()
        { 
        }

        public CosmosDBTrigger(string connectionString, string databaseName, string collectionName, string accountName)
        {
            CosmosDBConnectionString = connectionString;
            DatabaseName = databaseName;
            CollectionName = collectionName;
            AccountName = accountName;
        }
    }
}
using System;

namespace Keda.CosmosDB.Scaler.Services
{
    public class CosmosDBTrigger
    {
        public string CosmosDBConnectionString { get; internal set; }
        public string DatabaseName { get; internal set; }
        public string CollectionName { get; internal set; }
        public string AccountName { get; internal set; }
        public CosmosDBLease Lease { get; internal set; }

        public CosmosDBTrigger(string connectionString, string databaseName, string collectionName, string accountName)
        {
            CosmosDBConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            AccountName = accountName ?? throw new ArgumentNullException(nameof(accountName));
        }
    }
}
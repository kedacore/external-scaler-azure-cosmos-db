using System;
using System.Collections.Generic;


namespace Keda.Cosmosdb.Scaler.Services
{
    internal class CosmosDbTrigger
    {
        public string DocDBConnectionString { get; internal set; }
        public string LeasesDocDBConnectionString { get; internal set; }
        public string DatabaseName { get; internal set; }
        public string CollectionName { get; internal set; }
        public string LeaseDatabaseName { get; internal set; }
        public string LeaseCollectionName { get; internal set; }
        public string LeaseCollectionPrefix { get; internal set; }
    }

    internal class CosmosDbTriggerMetadata
    {
        public static string CollectionName { get { return "collectionName"; } }
        public static string DatabaseName { get { return "databaseName"; } }
        public static string DocDBConnectionString { get { return "docDBConnectionString"; } }
        public static string LeasesDocDBConnectionString { get { return "leasesDocDBConnectionString"; } }
        public static string LeaseDatabaseName { get { return "leaseDatabaseName"; } }
        public static string LeaseCollectionName { get { return "leaseCollectionName"; } }
        public static string LeaseCollectionPrefix { get { return "leaseCollectionPrefix"; } }
    }
}
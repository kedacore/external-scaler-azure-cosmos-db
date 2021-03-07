using System;
using System.Data.Common;
using Microsoft.Azure.Documents.ChangeFeedProcessor;


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
        public string AccountName { get; internal set; }
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
        public static string AccountName { get { return "accountName"; } }
    }

    internal class HostPropertiesCollection
    {
        public DocumentCollectionInfo DocumentCollectionLocation { get; private set; }

        public DocumentCollectionInfo LeaseCollectionLocation { get; private set; }

        public string HostName = "defaultName";

        public HostPropertiesCollection(DocumentCollectionInfo documentCollectionLocation, DocumentCollectionInfo leaseCollectionLocation)
        {
            this.DocumentCollectionLocation = documentCollectionLocation;
            this.LeaseCollectionLocation = leaseCollectionLocation;
        }
    }

    internal class DocumentDBConnectionString
    {
        public DocumentDBConnectionString(string connectionString)
        {
            // Use this generic builder to parse the connection string
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            object key = null;
            if (builder.TryGetValue("AccountKey", out key))
            {
                AuthKey = key.ToString();
            }

            object uri;
            if (builder.TryGetValue("AccountEndpoint", out uri))
            {
                ServiceEndpoint = new Uri(uri.ToString());
            }
        }

        public Uri ServiceEndpoint { get; set; }
        public string AuthKey { get; set; }
    }
}
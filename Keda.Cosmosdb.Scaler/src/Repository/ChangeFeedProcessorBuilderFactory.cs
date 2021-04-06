using Keda.CosmosDB.Scaler.Services;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.DataAccess;
using Microsoft.Azure.Documents.Client;

namespace Keda.Cosmosdb.Scaler.Repository
{
    public class ChangeFeedProcessorBuilderFactory
    {
        public static ChangeFeedProcessorBuilder GetBuilder(CosmosDBTrigger trigger)
        {
            CosmosDBConnectionString triggerConnection = new CosmosDBConnectionString(trigger.CosmosDBConnectionString);
            DocumentCollectionInfo documentCollectionLocation = new DocumentCollectionInfo
            {
                Uri = triggerConnection.ServiceEndpoint,
                MasterKey = triggerConnection.AuthKey,
                DatabaseName = trigger.DatabaseName,
                CollectionName = trigger.CollectionName
            };

            CosmosDBConnectionString leaseConnection = new CosmosDBConnectionString(trigger.Lease.LeasesCosmosDBConnectionString);

            DocumentCollectionInfo leaseCollectionLocation = new DocumentCollectionInfo
            {
                Uri = leaseConnection.ServiceEndpoint,
                MasterKey = leaseConnection.AuthKey,
                DatabaseName = trigger.Lease.LeaseDatabaseName,
                CollectionName = trigger.Lease.LeaseCollectionName
            };

            var changeFeedClient = new DocumentClient(documentCollectionLocation.Uri, documentCollectionLocation.MasterKey);
            IChangeFeedDocumentClient feedDocumentClient = new ChangeFeedDocumentClient(changeFeedClient);

            var leaseClient = new DocumentClient(leaseCollectionLocation.Uri, leaseCollectionLocation.MasterKey);
            IChangeFeedDocumentClient leaseDocumentClient = new ChangeFeedDocumentClient(leaseClient);

            var builder = new ChangeFeedProcessorBuilder()
                            .WithHostName(Constants.DefaultHostName)
                            .WithFeedCollection(documentCollectionLocation)
                            .WithLeaseCollection(leaseCollectionLocation)
                            .WithFeedDocumentClient(feedDocumentClient)
                            .WithLeaseDocumentClient(leaseDocumentClient);
            return builder;
        }
    }
}

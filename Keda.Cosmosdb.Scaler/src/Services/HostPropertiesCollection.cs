using Microsoft.Azure.Documents.ChangeFeedProcessor;

namespace Keda.CosmosDB.Scaler.Services
{
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
}

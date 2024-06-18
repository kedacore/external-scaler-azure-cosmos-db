using Microsoft.Extensions.Configuration;

namespace Keda.CosmosDb.Scaler.Demo.Shared
{
    public class CosmosDbConfig
    {
        public string Connection { get; set; }
        public string Endpoint { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public int ContainerThroughput { get; set; }

        public string LeaseConnection { get; set; }
        public string LeaseEndpoint { get; set; }
        public string LeaseDatabaseId { get; set; }
        public string LeaseContainerId { get; set; }
        public string ProcessorName { get; set; }

        public static CosmosDbConfig Create(IConfiguration configuration)
        {
            return configuration.GetSection(nameof(CosmosDbConfig)).Get<CosmosDbConfig>();
        }
    }
}

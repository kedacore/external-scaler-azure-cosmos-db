using System;
using System.Data.Common;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Keda.CosmosDb.Scaler
{
    [JsonObject(ItemRequired = Required.Always)]
    internal sealed class ScalerMetadata
    {
        private string _metricName;

        // Database & Container properties
        /// <summary>
        /// ID of Cosmos DB database containing monitored container.
        /// </summary>
        public string DatabaseId { get; set; }

        /// <summary>
        /// ID of monitored container.
        /// </summary>
        public string ContainerId { get; set; }

        /// <summary>
        /// ID of Cosmos DB database containing lease container.
        /// </summary>
        public string LeaseDatabaseId { get; set; }

        /// <summary>
        /// ID of lease container.
        /// </summary>
        public string LeaseContainerId { get; set; }

        // Connection String properties
        /// <summary>
        /// Environment variable for the connection string of Cosmos DB account with monitored container.
        /// </summary>
        [JsonProperty("ConnectionFromEnv", Required = Required.Default)]
        public string Connection { get; set; }

        /// <summary>
        /// Environment variable for the connection string of Cosmos DB account with lease container.
        /// </summary>
        [JsonProperty("LeaseConnectionFromEnv", Required = Required.Default)]
        public string LeaseConnection { get; set; }

        // Managed Identity properties
        /// <summary>
        /// Account endpoint of the CosmosDB account containing the monitored container.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string Endpoint { get; set; }

        /// <summary>
        /// Account endpoint of the CosmosDB account containing the lease container.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string LeaseEndpoint { get; set; }

        /// <summary>
        /// ClientId of the identity to be used. System assigned identity is used, if this is null.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ClientId { get; set; }

        /// <summary>
        /// Name of change-feed processor used by listener application.
        /// </summary>
        public string ProcessorName { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string MetricName
        {
            get
            {
                // Normalize metric name.
                _metricName ??=
                    $"cosmosdb-partitioncount-{this.LeaseAccountHost}-{this.LeaseDatabaseId}-{this.LeaseContainerId}-{this.ProcessorName}"
                    .Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-")
                    .ToLower();

                return _metricName;
            }

            set => _metricName = value;
        }

        [JsonIgnore]
        private string LeaseAccountHost
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LeaseEndpoint))
                {
                    return new Uri(LeaseEndpoint).Host;
                }
                else
                {
                    var builder = new DbConnectionStringBuilder { ConnectionString = LeaseConnection };
                    return new Uri((string)builder["AccountEndpoint"]).Host;
                }
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (string.IsNullOrEmpty(Connection) && string.IsNullOrEmpty(Endpoint))
            {
                throw new JsonSerializationException("Both Connection and Endpoint are missing.");
            }

            if (string.IsNullOrEmpty(LeaseConnection) && string.IsNullOrEmpty(LeaseEndpoint))
            {
                throw new JsonSerializationException("Both LeaseConnection and LeaseEndpoint are missing.");
            }
        }

        public static ScalerMetadata Create(ScaledObjectRef scaledObjectRef)
        {
            return JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());
        }
    }
}

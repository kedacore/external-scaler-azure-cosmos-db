using System;
using System.Data.Common;
using Newtonsoft.Json;

namespace Keda.CosmosDb.Scaler
{
    [JsonObject]
    internal sealed class ScalerMetadata
    {
        private string _metricName; // Private backing field for MetricName property
        private string _connection; // Private backing field for Connection property
        public string ConnectionFromEnv { get; set; }
        public string Connection
        {
            get => string.IsNullOrEmpty(_connection) ? ConnectionFromEnv : _connection;
            set => _connection = value;
        }
        [JsonProperty(Required = Required.Always)]
        public string DatabaseId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string ContainerId { get; set; }
        public string LeaseConnectionFromEnv { get; set; }
        private string _leaseConnection; // Private backing field for LeaseConnection property
        public string LeaseConnection
        {
            get => string.IsNullOrEmpty(_leaseConnection) ? LeaseConnectionFromEnv : _leaseConnection;
            set => _leaseConnection = value;
        }
        [JsonProperty(Required = Required.Always)]
        public string LeaseDatabaseId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string LeaseContainerId { get; set; }
        [JsonProperty(Required = Required.Always)]
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
                var builder = new DbConnectionStringBuilder { ConnectionString = this.LeaseConnection };
                return new Uri((string)builder["AccountEndpoint"]).Host;
            }
        }

        public static ScalerMetadata Create(ScaledObjectRef scaledObjectRef)
        {
            var metadata = JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());

            if (string.IsNullOrEmpty(metadata.Connection) && string.IsNullOrEmpty(metadata.ConnectionFromEnv))
            {
                throw new JsonSerializationException("Required property 'Connection' or 'ConnectionFromEnv' not found in JSON.");
            }

            if (string.IsNullOrEmpty(metadata.LeaseConnection) && string.IsNullOrEmpty(metadata.LeaseConnectionFromEnv))
            {
                throw new JsonSerializationException("Required property 'LeaseConnection' or 'LeaseConnectionFromEnv' not found in JSON.");
            }

            return metadata;
        }
    }
}

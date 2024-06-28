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

        [JsonProperty(Required = Required.Default)]
        public string Connection { get; set; }
        [JsonProperty(Required = Required.Default)]
        public string Endpoint { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        [JsonProperty(Required = Required.Default)]
        public string LeaseConnection { get; set; }
        [JsonProperty(Required = Required.Default)]
        public string LeaseEndpoint { get; set; }
        public string LeaseDatabaseId { get; set; }
        public string LeaseContainerId { get; set; }
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
                if (string.IsNullOrEmpty(LeaseConnection))
                {
                    return new Uri(LeaseEndpoint).Host;
                }
                var builder = new DbConnectionStringBuilder { ConnectionString = this.LeaseConnection };
                return new Uri((string)builder["AccountEndpoint"]).Host;
            }
        }

        public static ScalerMetadata Create(ScaledObjectRef scaledObjectRef)
        {
            return JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (string.IsNullOrEmpty(LeaseConnection) && string.IsNullOrEmpty(LeaseEndpoint))
            {
                throw new JsonSerializationException("Both LeaseConnection and LeaseEndpoint are missing.");
            }
            if(string.IsNullOrEmpty(Connection) && string.IsNullOrEmpty(Endpoint))
            {
                throw new JsonSerializationException("Both Connection and Endpoint are missing.");
            }
        }
    }
}

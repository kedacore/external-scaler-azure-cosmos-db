using System;
using System.Data.Common;
using Newtonsoft.Json;

namespace Keda.CosmosDbScaler
{
    [JsonObject(ItemRequired = Required.Always)]
    internal sealed class ScalerMetadata
    {
        public string Connection { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public string LeaseConnection { get; set; }
        public string LeaseDatabaseId { get; set; }
        public string LeaseContainerId { get; set; }
        public string ProcessorName { get; set; }

        // [JsonProperty(Required = Required.Default)]
        // public string MetricName { get; set; }

        [JsonIgnore]
        public string MetricName
        {
            get
            {
                string metricName =
                    $"cosmosdb-partitioncount-{this.LeaseAccountHost}-{this.LeaseDatabaseId}-{this.LeaseContainerId}-{this.ProcessorName}";

                // Normalize metric name.
                return metricName.Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-").ToLower();
            }
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
            return JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());
        }
    }
}

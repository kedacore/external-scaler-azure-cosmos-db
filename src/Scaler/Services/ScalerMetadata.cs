using System;
using System.Data.Common;
using Newtonsoft.Json;

namespace Keda.CosmosDb.Scaler
{
    [JsonObject(ItemRequired = Required.Always)]
    internal sealed class ScalerMetadata
    {
        private string _metricName;

        public string Connection { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
        public string LeaseConnection { get; set; }
        public string LeaseDatabaseId { get; set; }
        public string LeaseContainerId { get; set; }
        public string ProcessorName { get; set; }

        [JsonProperty(Required = Required.DisallowNull)]
        public string MetricName
        {
            get
            {
                if (_metricName == null)
                {
                    // Normalize metric name.
                    _metricName =
                        $"cosmosdb-partitioncount-{this.LeaseAccountHost}-{this.LeaseDatabaseId}-{this.LeaseContainerId}-{this.ProcessorName}"
                        .Replace("/", "-").Replace(".", "-").Replace(":", "-").Replace("%", "-")
                        .ToLower();
                }

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
            return JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());
        }
    }
}

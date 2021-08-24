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

        public static ScalerMetadata Create(ScaledObjectRef scaledObjectRef)
        {
            return JsonConvert.DeserializeObject<ScalerMetadata>(scaledObjectRef.ScalerMetadata.ToString());
        }
    }
}
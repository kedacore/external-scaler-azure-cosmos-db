using System.Threading.Tasks;

namespace Keda.CosmosDbScaler
{
    internal interface ICosmosDbMetricProvider
    {
        Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata);
    }
}
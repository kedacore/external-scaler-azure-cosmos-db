using System.Threading.Tasks;

namespace Keda.CosmosDb.Scaler
{
    internal interface ICosmosDbMetricProvider
    {
        Task<long> GetPartitionCountAsync(ScalerMetadata scalerMetadata);
    }
}
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public class CosmosDBRepository : ICosmosDBRepository
    {
        public async Task<long> GetEstimatedWork(ChangeFeedProcessorBuilder builder)
        {
            var workEstimator = await builder.BuildEstimatorAsync();
            long estimatedRemainingWork = await workEstimator.GetEstimatedRemainingWork();
            return estimatedRemainingWork;
        }
    }
}
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public class CosmosDBRepository : ICosmosDBRepository
    {
        private ILogger _logger;
        public CosmosDBRepository(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CosmosDBRepository>();
        }

        public async Task<long> GetEstimatedWork(ChangeFeedEstimator estimator)
        {
            using FeedIterator<ChangeFeedProcessorState> estimatorIterator = estimator.GetCurrentStateIterator();
            while (estimatorIterator.HasMoreResults)
            {
                FeedResponse<ChangeFeedProcessorState> states = await estimatorIterator.ReadNextAsync();
                foreach (ChangeFeedProcessorState leaseState in states)
                {
                    if (leaseState.EstimatedLag > 0)
                    {
                        _logger.LogDebug(
                            $"Lease {leaseState.LeaseToken} owned by host {leaseState.InstanceName ?? "None"} has an estimated lag of {leaseState.LeaseToken}");
                        return leaseState.EstimatedLag;
                    }
                }
            }
            return 0;
        }
    }
}
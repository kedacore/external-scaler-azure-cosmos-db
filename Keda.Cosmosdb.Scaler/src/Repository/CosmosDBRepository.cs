using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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
            List<ChangeFeedProcessorState> partitionWorkList = new List<ChangeFeedProcessorState>();

            using FeedIterator<ChangeFeedProcessorState> estimatorIterator = estimator.GetCurrentStateIterator();
            {
                while (estimatorIterator.HasMoreResults)
                {
                    FeedResponse<ChangeFeedProcessorState> response = await estimatorIterator.ReadNextAsync();
                    partitionWorkList.AddRange(response);
                }
            }
            return partitionWorkList.Sum(item => item.EstimatedLag); ;
        }
    }
}
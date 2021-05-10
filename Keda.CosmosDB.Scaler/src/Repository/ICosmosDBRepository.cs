using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public interface ICosmosDBRepository
    {
        Task<long> GetEstimatedWork(ChangeFeedEstimator estimator);
    }
}
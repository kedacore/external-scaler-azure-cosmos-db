using Keda.CosmosDB.Scaler.Services;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public interface ICosmosDBEstimator
    {
        Task<long> GetEstimatedWork(CosmosDBTrigger trigger);
    }
}
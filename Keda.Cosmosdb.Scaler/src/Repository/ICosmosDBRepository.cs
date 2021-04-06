using Microsoft.Azure.Documents.ChangeFeedProcessor;
using System.Threading.Tasks;

namespace Keda.CosmosDB.Scaler.Repository
{
    public interface ICosmosDBRepository
    {
        Task<long> GetEstimatedWork(ChangeFeedProcessorBuilder builder);
    }
}
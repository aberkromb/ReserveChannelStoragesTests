using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReserveChannelStoragesTests
{
    public interface IDataAccess<TIn, in TKey, TAddOut>
    {
        Task<TAddOut> Add(TIn @object, CancellationToken token);
        Task<TIn> Get(TKey key, CancellationToken token);
        Task<List<TIn>> GetAll(TKey key, CancellationToken token);
        Task<bool> Delete(TKey key, CancellationToken token);
        Task<List<TIn>> GetAllByCondition(TKey key ,CancellationToken token);
    }
}
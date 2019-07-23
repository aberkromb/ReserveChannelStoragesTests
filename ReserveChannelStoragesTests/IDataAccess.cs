using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReserveChannelStoragesTests
{
    public interface IDataAccess<TObj, in TKey>
    {
        Task<Unit> Add(TObj @object, CancellationToken token);
        Task<TObj> Get(TKey key, CancellationToken token);
        Task<List<TObj>> GetAll(TKey key, CancellationToken token);
        Task<bool> Delete(TKey key, CancellationToken token);
        Task<List<TObj>> GetAllByCondition(TKey key ,CancellationToken token);
    }
}
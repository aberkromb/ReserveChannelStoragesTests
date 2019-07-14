using System.Threading;
using System.Threading.Tasks;

namespace ReserveChannelStoragesTests
{
    public interface IDataAccess<TObj, in TKey>
    {
        Task<Unit> Add(TObj @object, CancellationToken token);
        Task<TObj> Get(TKey key, CancellationToken token);
    }
}
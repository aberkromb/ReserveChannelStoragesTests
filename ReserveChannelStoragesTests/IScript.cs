using System.Threading;
using System.Threading.Tasks;
using Generator;

namespace ReserveChannelStoragesTests
{
    public interface IScript
    {
        Task Write(MessageData data, CancellationToken cancellationToken);
        Task Read(CancellationToken cancellationToken);
        Task<int> AmountRemaining(CancellationToken cancellationToken);
    }
}
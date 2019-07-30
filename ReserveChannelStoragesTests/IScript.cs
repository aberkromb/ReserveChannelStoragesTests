using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Generator;

namespace ReserveChannelStoragesTests
{
    public interface IScript
    {
        Task Run(IEnumerable<MessageData> messages, CancellationToken cancellationToken);
    }
}
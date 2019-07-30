using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Generator;

namespace ReserveChannelStoragesTests.KafkaDataAccessImplementation
{
    public class KafkaSimpleWriteRead : IScript
    {
        public Task Run(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
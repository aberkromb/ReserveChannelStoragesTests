using System.Threading;
using System.Threading.Tasks;
using Generator;

namespace ReserveChannelStoragesTests.AerospikeDataAccessImplementation
{
    public class AerospikeSimpleWriteRead : IScript
    {
        public Task Write(MessageData data, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }


        public Task Read(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
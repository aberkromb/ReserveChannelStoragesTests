using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Generator;

namespace ReserveChannelStoragesTests.PostgresDataAccessImplementation
{
    public abstract class ScriptBase : IScript
    {
        protected Stopwatch stopwatch;
        protected bool isTimeInitialized;
        private readonly object locker = new object();


        protected ScriptBase(Stopwatch stopwatch, bool isTimeInitialized)
        {
            this.stopwatch = stopwatch;
            this.isTimeInitialized = isTimeInitialized;
        }


        protected abstract Task Write(IEnumerable<MessageData> datas, CancellationToken cancellationToken);
        protected abstract Task Read(CancellationToken cancellationToken);
        protected abstract Task<int> AmountRemaining(CancellationToken cancellationToken);
        public abstract Task Run(IEnumerable<MessageData> messageDatas, CancellationToken cancellationToken);
    }
}
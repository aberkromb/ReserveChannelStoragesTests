using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Rocks.Dataflow.Fluent;
using static ReserveChannelStoragesTests.Helpers;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.AerospikeDataAccessImplementation
{
    public class AerospikeSimpleWriteRead : IScript
    {
        private AerospikeDataAccess _dataAccess;
        private ScriptConfig _config;


        public AerospikeSimpleWriteRead(AerospikeDataAccess dataAccess, ScriptConfig config)
        {
            this._dataAccess = dataAccess;
            this._config = config;
        }


        public async Task Run(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Script running...{DateTimeFormatedString}");

            var writeCts = new CancellationTokenSource(this._config.TimeToWrite);
            var readCts = new CancellationTokenSource(this._config.TimeToRead);

            Task Write() => this.Write(messages, writeCts.Token);
            await Try(Write);

            Task Read() => this.Read(readCts.Token);
            await Try(Read);

            Console.WriteLine(await this.AmountRemaining(cancellationToken));
            Console.WriteLine($"Script ending...{DateTimeFormatedString}");
        }


        private Task Write(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start writing... {DateTimeFormatedString}");
            var dataflow = DataflowFluent
                           .ReceiveDataOfType<MessageData>()
                           .ProcessAsync(data => this._dataAccess.Add(data, CancellationToken.None))
                           .WithMaxDegreeOfParallelism(this._config.WriteParallelsCount)
                           .WithDefaultExceptionLogger((exception, o) => Console.WriteLine(exception))
                           .Action(x =>
                                   {
                                   })
                           .CreateDataflow();

            return MeasureIt(() => dataflow.ProcessAsync(messages, cancellationToken));
        }


        private async Task Read(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start reading... {DateTimeFormatedString}");
            Console.WriteLine(await MeasureIt(() => this._dataAccess.DeleteAll()));

//            while (!cancellationToken.IsCancellationRequested)
//            {
//                var batch = await this._dataAccess.GetBatch(this._config.GetBatchSize, cancellationToken);
//                if (batch.Count > 0)
//                {
//                    foreach (var messageData in batch)
//                        await this._dataAccess.Delete(this._dataAccess.CreateKey(messageData.Id), cancellationToken);
//                }
//                else
//                    break;
//            }
        }


        private async Task<int> AmountRemaining(CancellationToken cancellationToken) => (await this._dataAccess.GetAll(null, cancellationToken)).Count;
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Rocks.Dataflow;
using Rocks.Dataflow.Fluent;
using static ReserveChannelStoragesTests.Helpers;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;


namespace ReserveChannelStoragesTests.TarantoolDataAccessImplementation
{
    public class TarantoolSimpleWriteRead : IScript
    {
        private readonly TarantoolDataAccess _dataAccess;
        private ScriptConfig _config;


        public TarantoolSimpleWriteRead(TarantoolDataAccess dataAccess, ScriptConfig config)
        {
            this._dataAccess = dataAccess;
            this._config = config;
        }


        public async Task Run(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Script running...{DateTimeFormatedString}");

            var writeCts = new CancellationTokenSource(this._config.TimeToWrite);
            var readCts = new CancellationTokenSource(this._config.TimeToRead);

            Task Write() => this.WriteAll(messages, writeCts.Token);
            await Try(Write);

            Task Read() => this.Read(readCts.Token);
            await Try(Read);

            Console.WriteLine(await this.AmountRemaining(cancellationToken));
            Console.WriteLine($"Script ending...{DateTimeFormatedString}");
        }


        private Task WriteAll(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
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

            Task Func() => dataflow.ProcessAsync(messages, cancellationToken);

            return MeasureIt(Func);
        }


        private async Task Read(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start reading... {DateTimeFormatedString}");

            Task Func() => this.GetAndDeleteInternal(cancellationToken);
            await MeasureIt(Func);

            Console.WriteLine($"End reading... {DateTimeFormatedString}");
        }


        private async Task GetAndDeleteInternal(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = await this._dataAccess.GetBatch(this._config.GetBatchSize, cancellationToken);
                if (batch.Count > 0)
                    await this.CreateDeleteDataflow().ProcessAsync(batch, cancellationToken);
                else
                    break;
            }
        }


        private Dataflow<MessageData> CreateDeleteDataflow() =>
            DataflowFluent
                .ReceiveDataOfType<MessageData>()
                .ProcessAsync(data => this._dataAccess.Delete(data.Id, CancellationToken.None))
                .WithMaxDegreeOfParallelism(this._config.WriteParallelsCount)
                .WithDefaultExceptionLogger((exception, o) => Console.WriteLine(exception))
                .Action(x =>
                        {
                        })
                .CreateDataflow();


        private async Task<int> AmountRemaining(CancellationToken cancellationToken) => (await this._dataAccess.GetAll(int.MinValue, cancellationToken)).Count;
    }
}
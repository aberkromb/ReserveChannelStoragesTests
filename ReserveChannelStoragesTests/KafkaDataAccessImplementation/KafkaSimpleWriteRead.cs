using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using ReserveChannelStoragesTests.Telemetry;
using Rocks.Dataflow.Fluent;
using static ReserveChannelStoragesTests.Helpers;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;


namespace ReserveChannelStoragesTests.KafkaDataAccessImplementation
{
    public class KafkaSimpleWriteRead : IScript
    {
        private readonly KafkaDataAccess _dataAccess;
        private readonly ScriptConfig _config;


        public KafkaSimpleWriteRead(KafkaDataAccess dataAccess, ScriptConfig config)
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

            GetMeasurementsResult().ForEach(Console.WriteLine);
            
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

            return dataflow.ProcessAsync(messages, cancellationToken);
        }


        private async Task Read(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start reading... {DateTimeFormatedString}");

            Task Func() => this.ProcessAllMessages(cancellationToken);
            await MeasureIt(Func);

            Console.WriteLine($"End reading... {DateTimeFormatedString}");
        }


        private async Task ProcessAllMessages(CancellationToken cancellationToken)
        {
            async Task ProcessAllMessages()
            {
                while (true)
                {
                    var message = await this._dataAccess.Get(Unit.Value, CancellationToken.None);
                    if (message is null) break;
                }
            }

            List<Task> tasks = new List<Task>(this._config.ReadParallelsCount);
            for (var i = 0; i < this._config.ReadParallelsCount; i++)
                tasks.Add(Task.Run(ProcessAllMessages));

            await Task.WhenAll(tasks);

            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = await this._dataAccess.GetBatch(this._config.GetBatchSize, cancellationToken);
                if (batch.Count == 0) break;
            }
        }


        private async Task<int> AmountRemaining(CancellationToken cancellationToken) => (await this._dataAccess.GetAll(Unit.Value, cancellationToken)).Count;
    }
}
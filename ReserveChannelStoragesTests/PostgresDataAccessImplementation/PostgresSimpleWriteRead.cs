using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Rocks.Dataflow.Fluent;

namespace ReserveChannelStoragesTests.PostgresDataAccessImplementation
{
    public class PostgresSimpleWriteRead : ScriptBase
    {
        private PostgresDataAccess _postgres;
        private ScriptConfig _config;


        public PostgresSimpleWriteRead(PostgresDataAccess postgres, ScriptConfig config) : base(new Stopwatch(), false)
        {
            this._postgres = postgres;
            this._config = config;
        }


        public override async Task Run(IEnumerable<MessageData> datas, CancellationToken cancellationToken)
        {
            var writeCts = new CancellationTokenSource(this._config.TimeToWrite);
            var readCts = new CancellationTokenSource(this._config.TimeToRead);

            await Write(datas, writeCts.Token);
            await Read(readCts.Token);
            Console.WriteLine(await this.AmountRemaining(cancellationToken));
        }


        protected override Task Write(IEnumerable<MessageData> datas, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start writing... {DateTime.Now}");
            var dataflow = DataflowFluent
                           .ReceiveDataOfType<MessageData>()
                           .ProcessAsync(data => this._postgres.Add(data, CancellationToken.None))
                           .WithMaxDegreeOfParallelism(this._config.ParallelsCount)
                           .WithDefaultExceptionLogger((exception, o) => Console.WriteLine(exception))
                           .Action(x =>
                                   {
                                   })
                           .CreateDataflow();

            return dataflow.ProcessAsync(datas, cancellationToken);
        }


        protected override async Task Read(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start reading... {DateTime.Now}");
            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = await this._postgres.GetBatch(this._config.BatchSize, cancellationToken);
                if (batch.Count > 0)
                    await this._postgres.DeleteBatch(batch.Select(data => data.Id), cancellationToken);
                else
                    break;
            }
        }


        protected override async Task<int> AmountRemaining(CancellationToken cancellationToken) => (await this._postgres.GetAll(Guid.Empty, cancellationToken)).Count;
    }
}
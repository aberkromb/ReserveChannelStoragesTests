using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Rocks.Dataflow.Fluent;

namespace ReserveChannelStoragesTests.PostgresDataAccessImplementation
{
    public class PostgresSimpleWriteRead : IScript
    {
        private PostgresDataAccess _postgres;
        private ScriptConfig _config;


        public PostgresSimpleWriteRead(PostgresDataAccess postgres, ScriptConfig config)
        {
            this._postgres = postgres;
            this._config = config;
        }


        public async Task Run(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Script running...{Helpers.DateTime}");

            var writeCts = new CancellationTokenSource(this._config.TimeToWrite);
            var readCts = new CancellationTokenSource(this._config.TimeToRead);

            Task Write() => this.Write(messages, writeCts.Token);
            await Try(Write);
            
            Task Read() => this.Read(readCts.Token);
            await Try(Read);

            Console.WriteLine(await this.AmountRemaining(cancellationToken));
            Console.WriteLine($"Script ending...{Helpers.DateTime}");
        }


        private static async Task Try(Func<Task> func)
        {
            try
            {
                await func();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        private Task Write(IEnumerable<MessageData> messages, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start writing... {Helpers.DateTime}");
            var dataflow = DataflowFluent
                           .ReceiveDataOfType<MessageData>()
                           .ProcessAsync(data => this._postgres.Add(data, CancellationToken.None))
                           .WithMaxDegreeOfParallelism(this._config.ParallelsCount)
                           .WithDefaultExceptionLogger((exception, o) => Console.WriteLine(exception))
                           .Action(x =>
                                   {
                                   })
                           .CreateDataflow();

            return dataflow.ProcessAsync(messages, cancellationToken);
        }


        private async Task Read(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Start reading... {Helpers.DateTime}");
            while (!cancellationToken.IsCancellationRequested)
            {
                var batch = await this._postgres.GetBatch(this._config.BatchSize, cancellationToken);
                if (batch.Count > 0)
                    await this._postgres.DeleteBatch(batch.Select(data => data.Id), cancellationToken);
                else
                    break;
            }
        }


        private async Task<int> AmountRemaining(CancellationToken cancellationToken) => (await this._postgres.GetAll(Guid.Empty, cancellationToken)).Count;
    }
}
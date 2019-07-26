using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;

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


        public Task Write(MessageData data, CancellationToken cancellationToken) => this._postgres.Add(data, cancellationToken);


        public async Task Read(CancellationToken cancellationToken)
        {
            while (true)
            {
                var batch = await this._postgres.GetBatch(this._config.BatchSize, cancellationToken);
                if (batch.Count > 0)
                    await this._postgres.DeleteBatch(batch.Select(data => data.Id), cancellationToken);
                else
                    break;
            }
        }
    }
}
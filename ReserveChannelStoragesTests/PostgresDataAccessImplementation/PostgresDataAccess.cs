using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Npgsql;
using NpgsqlTypes;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.PostgresDataAccessImplementation
{
    // docker run --name some-postgres -p 5432:5432 -e POSTGRES_USER=test_user -e POSTGRES_PASSWORD=tests -d postgres
    public class PostgresDataAccess : IDataAccess<MessageData, Guid, Unit>
    {
        private static readonly string ConnectionString =
            "Host=localhost;Port=5432;Username=test_user;Password=tests;Database=postgres;Minimum Pool Size=200;Maximum Pool Size=1000";


        public Task<Unit> Add(MessageData @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);
            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(MessageData @object, CancellationToken token)
        {
            var commandText =
                "INSERT INTO reserve_channel_messages (id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl)"
                + "VALUES (@id, @message_date, @message_type, @additional_headers, @application, @exception, @exchange, @message, @message_routing_key, @persistent, @server, @ttl)";

            using var connection = await this.CreateConnection();
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", @object.Id));
            command.Parameters.Add(new NpgsqlParameter<DateTimeOffset>("@message_date", @object.MessageDate));
            command.Parameters.Add(new NpgsqlParameter<string>("@message_type", @object.MessageType));
            command.Parameters.Add(new NpgsqlParameter<string>("@additional_headers", @object.AdditionalHeaders));
            command.Parameters.Add(new NpgsqlParameter<string>("@application", @object.Application));
            command.Parameters.Add(new NpgsqlParameter<string>("@exception", @object.Exception));
            command.Parameters.Add(new NpgsqlParameter<string>("@exchange", @object.Exchange));
            command.Parameters.Add(new NpgsqlParameter<string>("@message", @object.Message));
            command.Parameters.Add(new NpgsqlParameter<string>("@message_routing_key", @object.MessageRoutingKey));
            command.Parameters.Add(new NpgsqlParameter<bool>("@persistent", @object.Persistent));
            command.Parameters.Add(new NpgsqlParameter<string>("@server", @object.Server));
            if (@object.Ttl.HasValue)
                command.Parameters.Add(new NpgsqlParameter<int>("@ttl", @object.Ttl.Value));

            await command.ExecuteNonQueryAsync(token);

            return Unit.Value;
        }


        public Task<MessageData> Get(Guid key, CancellationToken token)
        {
            Task<MessageData> Func() => GetInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<MessageData> GetInternal(Guid key, CancellationToken token)
        {
            var commandText =
                "select id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl from reserve_channel_messages where id = @id";

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", key));

            MessageData result = null;

            using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
                result = ToDataObject(reader);

            return result;
        }


        public Task<List<MessageData>> GetAll(Guid key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllInternal(token);
            return MeasureIt(Func);
        }


        private async Task<List<MessageData>> GetAllInternal(CancellationToken token)
        {
            var commandText =
                "select id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl from reserve_channel_messages";

            List<MessageData> result = new List<MessageData>();

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                result.Add(ToDataObject(reader));

            return result;
        }


        public Task<List<MessageData>> GetBatch(int count, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetBatchInternal(count, token);
            return MeasureIt(Func);
        }


        private async Task<List<MessageData>> GetBatchInternal(int count, CancellationToken token)
        {
            var commandText =
                "select id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl from reserve_channel_messages limit " +
                count;

            List<MessageData> result = new List<MessageData>();

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
                result.Add(ToDataObject(reader));

            return result;
        }


        public Task<bool> Delete(Guid key, CancellationToken token)
        {
            Task<bool> Func() => this.DeleteInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<bool> DeleteInternal(Guid key, CancellationToken token)
        {
            var commandText = "delete from reserve_channel_messages where id = @id";

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", key));

            await command.ExecuteNonQueryAsync(token);

            return true;
        }


        public Task<bool> DeleteBatch(IEnumerable<Guid> keys, CancellationToken token)
        {
            Task<bool> Func() => this.DeleteBatchInternal(keys, token);
            return MeasureIt(Func);
        }


        private async Task<bool> DeleteBatchInternal(IEnumerable<Guid> keys, CancellationToken token)
        {
            var commandText = "delete from reserve_channel_messages where id = ANY(@ids)";

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            command.Parameters.Add("@ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid).Value = keys.ToArray();

            await command.ExecuteNonQueryAsync(token);

            return true;
        }


        public Task<List<MessageData>> GetAllByCondition(Guid key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllByConditionInternal(token);
            return MeasureIt(Func);
        }


        private async Task<List<MessageData>> GetAllByConditionInternal(CancellationToken token)
        {
            var commandText = "delete from reserve_channel_messages where message_date < @date";

            using var connection = await this.CreateConnection();
            using var command = new NpgsqlCommand(commandText, connection);
            command.Parameters.Add(new NpgsqlParameter<DateTime>("@date", DateTime.Now.AddDays(-15)));

            await command.ExecuteNonQueryAsync(token);

            return await this.GetAllInternal(token);
        }


        private static MessageData ToDataObject(DbDataReader reader)
        {
            var result = new MessageData
                         {
                             Id = reader.GetGuid(0),
                             MessageDate = reader.GetDateTime(1),
                             MessageType = reader.GetString(2),
                             AdditionalHeaders = reader.GetString(3),
                             Application = reader.GetString(4),
                             Exception = reader.GetString(5),
                             Exchange = reader.GetString(6),
                             Message = reader.GetString(7),
                             MessageRoutingKey = reader.GetString(8),
                             Persistent = reader.GetBoolean(9),
                             Server = reader.GetString(10),
                             Ttl = reader.IsDBNull(11) ? (int?) null : reader.GetInt32(11)
                         };

            return result;
        }


        private async Task<NpgsqlConnection> CreateConnection()
        {
            var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
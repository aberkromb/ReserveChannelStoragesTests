using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using Npgsql;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.PostgresDataAccessImplementation
{
    // docker run --name some-postgres -p 5432:5432 -e POSTGRES_USER=test_user -e POSTGRES_PASSWORD=tests -e POSTGRES_DB=RocksSqlMetalTestDatabase -d postgres
    public class PostgresDataAccess : IDataAccess<PostgresDataObject, Guid>
    {
        private static readonly string ConnectionString = "Host=localhost;Port=5432;Username=test_user;Password=tests;Database=postgres";

        private readonly NpgsqlConnection _connection;


        public PostgresDataAccess()
        {
            this._connection = new NpgsqlConnection(ConnectionString);
        }


        public Task<Unit> Add(PostgresDataObject @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);
            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(PostgresDataObject @object, CancellationToken token)
        {
            var commandText =
                "INSERT INTO reserve_channel_messages (id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl)"
                + "VALUES (@id, @message_date, @message_type, @additional_headers, @application, @exception, @exchange, @message, @message_routing_key, @persistent, @server, @ttl)";

            await OpenConnection();
            using var command = new NpgsqlCommand(commandText, this._connection);
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", @object.DataObject.Id));
            command.Parameters.Add(new NpgsqlParameter<DateTimeOffset>("@message_date", @object.DataObject.MessageDate));
            command.Parameters.Add(new NpgsqlParameter<string>("@message_type", @object.DataObject.MessageType));
            command.Parameters.Add(new NpgsqlParameter<string>("@additional_headers", @object.DataObject.AdditionalHeaders));
            command.Parameters.Add(new NpgsqlParameter<string>("@application", @object.DataObject.Application));
            command.Parameters.Add(new NpgsqlParameter<string>("@exception", @object.DataObject.Exception));
            command.Parameters.Add(new NpgsqlParameter<string>("@exchange", @object.DataObject.Exchange));
            command.Parameters.Add(new NpgsqlParameter<string>("@message", @object.DataObject.Message));
            command.Parameters.Add(new NpgsqlParameter<string>("@message_routing_key", @object.DataObject.MessageRoutingKey));
            command.Parameters.Add(new NpgsqlParameter<bool>("@persistent", @object.DataObject.Persistent));
            command.Parameters.Add(new NpgsqlParameter<string>("@server", @object.DataObject.Server));
            if (@object.DataObject.Ttl.HasValue)
                command.Parameters.Add(new NpgsqlParameter<int>("@ttl", @object.DataObject.Ttl.Value));

            await command.ExecuteNonQueryAsync(token);

            return Unit.Value;
        }


        public Task<PostgresDataObject> Get(Guid key, CancellationToken token)
        {
            Task<PostgresDataObject> Func() => GetInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<PostgresDataObject> GetInternal(Guid key, CancellationToken token)
        {
            var commandText =
                "select id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl from reserve_channel_messages where id = @id";

            await OpenConnection();
            using var command = new NpgsqlCommand(commandText, this._connection);
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", key));

            PostgresDataObject result = null;

            using var reader = await command.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
                result = ToDataObject(reader);

            return result;
        }


        public Task<List<PostgresDataObject>> GetAll(Guid key, CancellationToken token)
        {
            Task<List<PostgresDataObject>> Func() => GetAllInternal(token);
            return MeasureIt(Func);
        }


        private async Task<List<PostgresDataObject>> GetAllInternal(CancellationToken token)
        {
            var commandText =
                "select id, message_date, message_type, additional_headers, application, exception,exchange, message, message_routing_key, persistent, server, ttl from reserve_channel_messages";

            List<PostgresDataObject> result = new List<PostgresDataObject>();

            await OpenConnection();
            using var command = new NpgsqlCommand(commandText, this._connection);
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

            await OpenConnection();
            using var command = new NpgsqlCommand(commandText, this._connection);
            command.Parameters.Add(new NpgsqlParameter<Guid>("@id", key));

            await command.ExecuteNonQueryAsync(token);

            return true;
        }


        private static PostgresDataObject ToDataObject(DbDataReader reader)
        {
            var result = new PostgresDataObject();

            result.DataObject = new MessageData
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


        private Task OpenConnection()
        {
            if (this._connection.State != ConnectionState.Open)
                return this._connection.OpenAsync();

            return Task.CompletedTask;
        }
    }
}
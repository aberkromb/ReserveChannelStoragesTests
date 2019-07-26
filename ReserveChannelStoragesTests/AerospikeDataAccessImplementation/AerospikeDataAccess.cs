using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.BinarySerializers;
using static ReserveChannelStoragesTests.Filters;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests
{
    // docker run -tid --name aerospike -e "NAMESPACE=reserve_channel" -p 3000:3000 -p 3001:3001 -p 3002:3002 -p 3003:3003 aerospike/aerospike-server

    public class AerospikeDataAccess : IDataAccess<MessageData, Key, Unit>
    {
        private static readonly string Hostname = "192.168.99.100";

        private AsyncClient _client;
        private readonly IBinarySerializer _binarySerializer;

        private WritePolicy _writePolicy;
        private Policy _policy;
        private ScanPolicy _scanPolicy;

        private const string defaultBinName = "msg";
        private const string defaultNs = "reserve_channel";
        private const string defaultSetName = "messages";


        public AerospikeDataAccess(IBinarySerializer binarySerializer)
        {
            this._binarySerializer = binarySerializer;

            _client = new AsyncClient(Hostname, 3000);

            _writePolicy = new WritePolicy();
            _policy = new Policy();
            _scanPolicy = new ScanPolicy();
        }


        public Task<Unit> Add(MessageData @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);

            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(MessageData @object, CancellationToken token)
        {
            var key = new Key(defaultNs, defaultSetName, this._binarySerializer.Serialize(@object.Id));
            var bin = new Bin(defaultBinName, _binarySerializer.Serialize(@object));

            await _client.Put(_writePolicy, token, key, bin);

            return Unit.Value;
        }


        public Task<MessageData> Get(Key key, CancellationToken token)
        {
            Task<MessageData> Func() => GetInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<MessageData> GetInternal(Key key, CancellationToken token)
        {
            var record = await _client.Get(_policy, token, key);
            return ConvertRecord(record);
        }


        public async Task<List<MessageData>> GetAll(Key key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllInternal(key, token);
            return await MeasureIt(Func);
        }


        public Task<List<MessageData>> GetBatch(int count, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        private Task<List<MessageData>> GetAllInternal(Key key, CancellationToken token)
        {
            var list = new List<MessageData>();
            _client.ScanAll(_scanPolicy,
                            key.ns,
                            key.setName,
                            (key1, record) => list.Add(GetAerospikeDataObjectFrom(key1, record).Data));

            return Task.FromResult(list);
        }


        public Task<bool> Delete(Key key, CancellationToken token)
        {
            Task<bool> Func() => DeleteInternal(key, token);
            return MeasureIt(Func);
        }


        public Task<bool> DeleteBatch(IEnumerable<Key> keys, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        private Task<bool> DeleteInternal(Key key, CancellationToken token) => _client.Delete(_writePolicy, token, key);


        public Task<List<MessageData>> GetAllByCondition(Key key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllByConditionInternal(key);
            return MeasureIt(Func);
        }


        private Task<List<MessageData>> GetAllByConditionInternal(Key k)
        {
            var list = new List<MessageData>();
            this._client.ScanAll(this._scanPolicy,
                                 k.ns,
                                 k.setName,
                                 (key, record) =>
                                 {
                                     var obj = ConvertRecord(record);
                                     if (IsFiltersPassed(obj))
                                         list.Add(obj);
                                 });

            return Task.FromResult(list);
        }


        private AerospikeDataObject GetAerospikeDataObjectFrom(Key key, Record record)
        {
            var data = ConvertRecord(record);
            return new AerospikeDataObject { Data = data, Key = key.userKey?.ToInteger(), Namespace = key.ns, SetName = key.setName };
        }


        private MessageData ConvertRecord(Record record) => _binarySerializer.Deserialize<MessageData>((byte[]) record.GetValue(defaultBinName));
       
    }
}
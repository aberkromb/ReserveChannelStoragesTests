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
        private AsyncClient _client;
        private readonly IBinarySerializer _binarySerializer;

        private readonly WritePolicy _writePolicy;
        private readonly Policy _policy;
        private readonly ScanPolicy _scanPolicy;

//        private static readonly string Hostname = "192.168.99.100";
        private static readonly string Hostname = "localhost";
        private const string defaultBinName = "msg";
        private const string defaultNs = "reserve_channel";
        private const string defaultSetName = "messages";


        public AerospikeDataAccess(IBinarySerializer binarySerializer)
        {
            this._binarySerializer = binarySerializer;

            _client = new AsyncClient(Hostname, 3000);

            _writePolicy = new WritePolicy();
            _policy = new Policy();
            _scanPolicy = new ScanPolicy { scanPercent = 10 };
        }


        public Task<Unit> Add(MessageData @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);

            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(MessageData @object, CancellationToken token)
        {
            var key = this.CreateKey(@object);
            var bin = new Bin(defaultBinName, _binarySerializer.Serialize(@object));

            await _client.Put(_writePolicy, token, key, bin);

            return Unit.Value;
        }


        public Key CreateKey(MessageData @object) => new Key(defaultNs, defaultSetName, this._binarySerializer.Serialize(@object.Id));


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


        public Task<List<MessageData>> GetBatch(int count, CancellationToken token)
        {
            var scanPolicy = new ScanPolicy { scanPercent = count };
            var list = new List<MessageData>();

            this.ScanAllInternal(scanPolicy, list);

            return Task.FromResult(list);
        }


        public async Task<List<MessageData>> GetAll(Key key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllInternal(key, token);
            return await MeasureIt(Func);
        }


        private Task<List<MessageData>> GetAllInternal(Key __, CancellationToken _)
        {
            var scanPolicy = new ScanPolicy();
            var list = new List<MessageData>();

            this.ScanAllInternal(scanPolicy, list);

            return Task.FromResult(list);
        }


        private void ScanAllInternal(ScanPolicy scanPolicy, List<MessageData> list)
        {
            this._client.ScanAll(scanPolicy,
                                 defaultNs,
                                 defaultSetName,
                                 (key1, record) => list.Add(this.GetAerospikeDataObjectFrom(key1, record).Data));
        }


        public Task<bool> Delete(Key key, CancellationToken token)
        {
            Task<bool> Func() => DeleteInternal(key, token);
            return MeasureIt(Func);
        }


        public async Task<bool> DeleteBatch(IEnumerable<Key> keys, CancellationToken token)
        {
            foreach (var key in keys)
                await this._client.Delete(null, token, key);

            return true;
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
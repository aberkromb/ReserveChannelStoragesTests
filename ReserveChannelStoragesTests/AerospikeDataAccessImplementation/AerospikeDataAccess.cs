using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.BinarySerializers;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests
{
    // docker run -tid --name aerospike -e "NAMESPACE=reserve_channel" -p 3000:3000 -p 3001:3001 -p 3002:3002 -p 3003:3003 aerospike/aerospike-server

    public class AerospikeDataAccess : IDataAccess<AerospikeDataObject, Key, Unit>
    {
        private static readonly string Hostname = "192.168.99.100";

        private AsyncClient _client;
        private readonly IBinarySerializer _binarySerializer;

        private WritePolicy _writePolicy;
        private Policy _policy;
        private ScanPolicy _scanPolicy;

        private const string defaultBinName = "msg";


        public AerospikeDataAccess(IBinarySerializer binarySerializer)
        {
            this._binarySerializer = binarySerializer;

            _client = new AsyncClient(Hostname, 3000);

            _writePolicy = new WritePolicy();
            _policy = new Policy();
            _scanPolicy = new ScanPolicy();
        }


        public Task<Unit> Add(AerospikeDataObject @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);

            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(AerospikeDataObject @object, CancellationToken token)
        {
            var key = new Key(@object.Namespace, @object.SetName, @object.Key.Value);
            var bin = new Bin(defaultBinName, _binarySerializer.Serialize(@object.Data));

            await _client.Put(_writePolicy, token, key, bin);

            return Unit.Value;
        }


        public Task<AerospikeDataObject> Get(Key key, CancellationToken token)
        {
            Task<AerospikeDataObject> Func() => GetInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<AerospikeDataObject> GetInternal(Key key, CancellationToken token)
        {
            var record = await _client.Get(_policy, token, key);
            return GetAerospikeDataObjectFrom(key, record);
        }


        public async Task<List<AerospikeDataObject>> GetAll(Key key, CancellationToken token)
        {
            Task<List<AerospikeDataObject>> Func() => GetAllInternal(key, token);
            return await MeasureIt(Func);
        }


        private Task<List<AerospikeDataObject>> GetAllInternal(Key key, CancellationToken token)
        {
            var list = new List<AerospikeDataObject>();
            _client.ScanAll(_scanPolicy,
                            key.ns,
                            key.setName,
                            (key1, record) => list.Add(GetAerospikeDataObjectFrom(key1, record)));

            return Task.FromResult(list);
        }


        public Task<bool> Delete(Key key, CancellationToken token)
        {
            Task<bool> Func() => DeleteInternal(key, token);
            return MeasureIt(Func);
        }


        private Task<bool> DeleteInternal(Key key, CancellationToken token) => _client.Delete(_writePolicy, token, key);


        public Task<List<AerospikeDataObject>> GetAllByCondition(Key key, CancellationToken token)
        {
            Task<List<AerospikeDataObject>> Func() => GetAllByConditionInternal(key);
            return MeasureIt(Func);
        }


        private Task< List<AerospikeDataObject>> GetAllByConditionInternal(Key key)
        {
            var dt = DateTime.Now.AddDays(-15);
            bool Predicate(MessageData data) => data.MessageDate < dt;

            var list = new List<AerospikeDataObject>();
            this._client.ScanAll(this._scanPolicy,
                            key.ns,
                            key.setName,
                            (key1, record) =>
                            {
                                var obj = this.GetAerospikeDataObjectFrom(key1, record);
                                if (Predicate(obj.Data))
                                    list.Add(obj);
                            });

            return Task.FromResult(list);
        }


        private AerospikeDataObject GetAerospikeDataObjectFrom(Key key, Record record)
        {
            var data = (byte[]) record.GetValue(defaultBinName);
            return new AerospikeDataObject
                   { Data = _binarySerializer.Deserialize<MessageData>(data), Key = key.userKey?.ToInteger(), Namespace = key.ns, SetName = key.setName };
        }
    }
}
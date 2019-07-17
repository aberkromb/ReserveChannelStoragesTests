using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests
{
    // docker run -tid --name aerospike -e "NAMESPACE=reserve_channel" -p 3000:3000 -p 3001:3001 -p 3002:3002 -p 3003:3003 aerospike/aerospike-server

    public class AerospikeDataAccess : IDataAccess<AerospikeDataObject, Key>
    {
        private AsyncClient _client;

        private WritePolicy _writePolicy;
        private Policy _policy;
        private ScanPolicy _scanPolicy;

        private const string defaultBinName = "msg";


        public AerospikeDataAccess(AsyncClient client)
        {
            _client = client;

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
            var bin = new Bin(defaultBinName, @object.Data);

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

        private static AerospikeDataObject GetAerospikeDataObjectFrom(Key key, Record record)
        {
            var data = (byte[]) record.GetValue(defaultBinName);
            return new AerospikeDataObject
                {Data = data, Key = key.userKey?.ToInteger(), Namespace = key.ns, SetName = key.setName};
        }

        public async Task<List<AerospikeDataObject>> GetAll(Key key, CancellationToken token)
        {
            Task<List<AerospikeDataObject>> Func() => GetAllInternal(key, token);
            return await MeasureIt(Func);
        }

        private Task<List<AerospikeDataObject>> GetAllInternal(Key key, CancellationToken token)
        {
            var list = new List<AerospikeDataObject>();
            _client.ScanAll(_scanPolicy, key.ns, key.setName,
                (key1, record) => list.Add(GetAerospikeDataObjectFrom(key1, record)));

            return Task.FromResult(list);
        }
    }
}
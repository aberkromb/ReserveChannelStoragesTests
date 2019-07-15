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


        public AerospikeDataAccess(AsyncClient client)
        {
            _client = client;

            _writePolicy = new WritePolicy();
            _policy = new Policy();
        }


        public Task<Unit> Add(AerospikeDataObject @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);

            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(AerospikeDataObject @object, CancellationToken token)
        {
            var key = new Key(@object.Namespace, @object.SetName, @object.Key);
            var bin = new Bin("msg", @object.Data);

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
            var data = (byte[]) record.GetValue("msg");
            return new AerospikeDataObject
                {Data = data, Key = key.userKey.ToInteger(), Namespace = key.ns, SetName = key.setName};
        }


        public async Task<List<AerospikeDataObject>> GetAll(Key key, CancellationToken token)
        {
            var records = _client.ScanAll(new ScanPolicy(), key.ns, key.setName, );
        }
        
    }
}
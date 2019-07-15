using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Newtonsoft.Json;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.JsonSerializers;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests
{
    // docker run -tid --name aerospike -e "NAMESPACE=reserve_channel" -p 3000:3000 -p 3001:3001 -p 3002:3002 -p 3003:3003 aerospike/aerospike-server

    public class AerospikeDataProvider : IDataAccess<AerospikeDataObject, Key>
    {
        private AsyncClient _client;

        private WritePolicy _writePolicy;
        private Policy _policy;

        private IJsonService _jsonService;


        public AerospikeDataProvider(AsyncClient client, IJsonService jsonService)
        {
            _client = client;
            this._jsonService = jsonService;

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
            
            return new AerospikeDataObject{Data = data, Key = key.userKey.ToInteger(), Namespace = key.ns, SetName = key.setName};
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Newtonsoft.Json;

namespace ReserveChannelStoragesTests.AerospikeDataAccessImplementation
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


        public async Task<Unit> Add(AerospikeDataObject @object, CancellationToken token)
        {
            var key = new Key(@object.Namespace, @object.SetName, @object.Key);
            var bin = new Bin("msg", JsonConvert.SerializeObject(@object));
            await _client.Put(_writePolicy, token, key, bin);
            
            return Unit.Value;
        }


        public async Task<AerospikeDataObject> Get(Key key, CancellationToken token)
        {
            var record = await _client.Get(_policy, token, key);
            return JsonConvert.DeserializeObject<AerospikeDataObject>(record.GetString("msg"));
        }
    }
}
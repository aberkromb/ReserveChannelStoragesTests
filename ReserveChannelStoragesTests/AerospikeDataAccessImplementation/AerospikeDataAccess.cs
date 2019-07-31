using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using Newtonsoft.Json;
using ReserveChannelStoragesTests.BinarySerializers;
using static ReserveChannelStoragesTests.Filters;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests
{
    // docker run -tid -d -v D:\as:/opt/aerospike/data --name aerospike -e "MEM_GB=2" -e "STORAGE_GB=100" -e "NAMESPACE=reserve_channel" -p 3000:3000 -p 3001:3001 -p 3002:3002 -p 3003:3003 aerospike/aerospike-server

    public class AerospikeDataAccess
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

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
            _scanPolicy = new ScanPolicy();
        }


        public Task<Unit> Add(MessageData @object, CancellationToken token)
        {
            Task<Unit> Func() => AddInternal(@object, token);
            return MeasureIt(Func);
        }


        private async Task<Unit> AddInternal(MessageData @object, CancellationToken token)
        {
            var key = CreateKey();
            var bin = new Bin(@object.Id.ToString(), this._binarySerializer.Serialize(@object));

            await _client.Put(_writePolicy, token, key, bin);

            return Unit.Value;
        }


        public static Key CreateKey() => new Key(defaultNs, defaultSetName, DateTimeOffset.Now.ToUnixTimeMilliseconds());


        public Task<List<MessageData>> Get(Key key, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetInternal(key, token);
            return MeasureIt(Func);
        }


        private async Task<List<MessageData>> GetInternal(Key key, CancellationToken token)
        {
            var record = await _client.Get(_policy, token, key);
            return this.ConvertRecordToMessages(record);
        }


        public Task<List<MessageData>> GetBatch(int count, CancellationToken token)
        {
            Task<List<MessageData>> Func() => this.GetBatchInternal(count);
            return MeasureIt(Func);
        }


        readonly ScanPolicy _getBatchScanPolicy = new ScanPolicy { includeBinData = false };


        private async Task<List<MessageData>> GetBatchInternal(int count)
        {
            var keys = new List<Key>();

            try
            {
                this._client.ScanAll(_getBatchScanPolicy,
                                     defaultNs,
                                     defaultSetName,
                                     async (key, record) =>
                                     {
                                         keys.Add(key);

                                         await Delete(key, CancellationToken.None);
                                         
                                         if(keys.Count >= count)
                                             throw new AerospikeException("");
                                     });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            count = keys.Count > count ? count : keys.Count;

            var result = new List<MessageData>();
            for (var i = 0; i < count; i++)
                result.AddRange(await Get(keys[i], CancellationToken.None));

            return result;
        }


        public async Task<List<MessageData>> GetAll(Key _, CancellationToken token)
        {
            Task<List<MessageData>> Func() => GetAllInternal(_, token);
            return await MeasureIt(Func);
        }


        private Task<List<MessageData>> GetAllInternal(Key __, CancellationToken _)
        {
            var list = new List<MessageData>();

            this.ScanAll(_scanPolicy, list);

            return Task.FromResult(list);
        }


        private void ScanAll(ScanPolicy scanPolicy, List<MessageData> list)
        {
            this._client.ScanAll(scanPolicy,
                                 defaultNs,
                                 defaultSetName,
                                 (_, record) => list.AddRange(ConvertRecordToMessages(record)));
        }


        public Task<int> DeleteAll()
        {
            var keys = new List<Key>();

            this._client.ScanAll(_getBatchScanPolicy,
                                 defaultNs,
                                 defaultSetName,
                                 (key, record) => keys.Add(key));

            foreach (var key in keys)
            {
                this._client.Delete(this._writePolicy, key);
            }

            return Task.FromResult<int>(keys.Count);
        }


        public Task<bool> Delete(Key key, CancellationToken token)
        {
            Task<bool> Func() => DeleteInternal(key, token);
            return MeasureIt(Func);
        }


        private Task<bool> DeleteInternal(Key key, CancellationToken token) => _client.Delete(_writePolicy, token, key);


        public Task<bool> DeleteBatch(IEnumerable<Key> keys, CancellationToken token)
        {
            async Task<bool> DeleteBatchInternal()
            {
                foreach (var key in keys)
                    await this._client.Delete(null, token, key);
                return true;
            }

            return MeasureIt(DeleteBatchInternal);
        }


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
                                     var messages = this.ConvertRecordToMessages(record);
                                     foreach (var message in messages)
                                     {
                                         if (IsFiltersPassed(message))
                                             list.Add(message);
                                     }
                                 });

            return Task.FromResult(list);
        }


        private List<MessageData> ConvertRecordToMessages(Record record)
        {
            var list = new List<MessageData>(record.bins.Count);

            foreach (var bin in record.bins)
                list.Add(this._binarySerializer.Deserialize<MessageData>((byte[]) record.GetValue(bin.Key)));

            return list;
        }
    }
}
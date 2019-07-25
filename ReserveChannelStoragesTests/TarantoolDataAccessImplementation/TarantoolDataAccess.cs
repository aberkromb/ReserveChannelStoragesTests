using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using ProGaudi.Tarantool.Client;
using ProGaudi.Tarantool.Client.Model;
using ProGaudi.Tarantool.Client.Model.Enums;
using ProGaudi.Tarantool.Client.Model.Responses;
using ReserveChannelStoragesTests.JsonSerializers;
using static ReserveChannelStoragesTests.Filters;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace ReserveChannelStoragesTests.TarantoolDataAccessImplementation
{
//    docker run --name mytarantool -p 3301:3301 -d tarantool/tarantool
// docker exec -i -t mytarantool console
// s = box.schema.space.create('reservechannel', {id = 999, field_count = 2, engine = 'vinyl', format = {{name='id', type = 'integer'}, {name='data', type='string'}} })
// seq = box.schema.sequence.create('seq')
// s:create_index('Q',{sequence='seq'})
    public class TarantoolDataAccess : IDataAccess<TarantoolDataObject, long, long>
    {
        private readonly Box _box;
        private readonly ISpace _space;
        private readonly IJsonService _jsonService;


        public TarantoolDataAccess(IJsonService jsonService)
        {
            this._jsonService = jsonService;
            _box = Box.Connect("localhost", 3301, "guest", string.Empty).GetAwaiter().GetResult();
            this._space = this._box.GetSchema()["reservechannel"];
        }


        public Task<long> Add(TarantoolDataObject @object, CancellationToken token)
        {
            async Task<long> Func()
            {
                var response = await this._space.Insert(TarantoolTuple.Create((long?) null, this._jsonService.Serialize(@object.Data)));
                var x = response.Data[0].Item1;
                return x.Value;
            }

            return MeasureIt(Func);
        }


        public Task<TarantoolDataObject> Get(long key, CancellationToken token)
        {
            Task<TarantoolDataObject> Func() => GetInternal(key);
            return MeasureIt(Func);
        }


        private async Task<TarantoolDataObject> GetInternal(long key)
        {
            var result = await this._space.Get<ValueTuple<long>, ValueTuple<long, string>>(ValueTuple.Create(key));
            return new TarantoolDataObject { Data = this._jsonService.Deserialize<MessageData>(result.Item2) };
        }


        public Task<List<TarantoolDataObject>> GetAll(long key, CancellationToken token)
        {
            Task<List<TarantoolDataObject>> Func() => this.GetAllInternal();
            return MeasureIt(Func);
        }


        private async Task<List<TarantoolDataObject>> GetAllInternal()
        {
            var response = await this._space["Q"].Select<ValueTuple<long>, ValueTuple<long, string>>(ValueTuple.Create(-1L), new SelectOptions { Iterator = Iterator.All });

            var result = response.Data.Aggregate(new List<TarantoolDataObject>(response.Data.Length),
                                                 (list, s) =>
                                                 {
                                                     list.Add(new TarantoolDataObject { Data = this._jsonService.Deserialize<MessageData>(s.Item2) });
                                                     return list;
                                                 });
            return result;
        }


        public async Task<bool> Delete(long key, CancellationToken token)
        {
            Task<DataResponse<(long, string)[]>> Func() =>
                this._space.Delete<ValueTuple<long>, ValueTuple<long, string>>(ValueTuple.Create(key));

            await MeasureIt(Func);

            return true;
        }


        public Task<List<TarantoolDataObject>> GetAllByCondition(long _, CancellationToken token)
        {
            Task<List<TarantoolDataObject>> Func() => this.GetAllByConditionInternal();

            return MeasureIt(Func);
        }


        private async Task<List<TarantoolDataObject>> GetAllByConditionInternal()
        {
            var response = await this._space["Q"].Select<long, string>(-1L, new SelectOptions { Iterator = Iterator.All });

            var result = response.Data.Aggregate(new List<TarantoolDataObject>(response.Data.Length),
                                                 (list, s) =>
                                                 {
                                                     var obj = this._jsonService.Deserialize<MessageData>(s);
                                                     if (IsFiltersPassed(obj))
                                                         list.Add(new TarantoolDataObject { Data = obj });
                                                     return list;
                                                 });

            return result;
        }
    }

    public class TarantoolDataObject : DataObjectBase
    {
        public long? Id { get; set; }
    }
}
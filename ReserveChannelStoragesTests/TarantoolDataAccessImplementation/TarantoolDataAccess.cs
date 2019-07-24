using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ProGaudi.Tarantool.Client;
using ReserveChannelStoragesTests.JsonSerializers;

namespace ReserveChannelStoragesTests.TarantoolDataAccessImplementation
{
//    docker run --name mytarantool -p3301:3301 -d tarantool/tarantool
// s = box.schema.space.create('reservechannel', {id = 999, field_count = 2, engine = 'vinyl', format = {{name='id', type = 'integer'}, {name='data', type='string'}} })
// seq = box.schema.sequence.create('seq')
// s:create_index('Q',{sequence='seq'})
    public class TarantoolDataAccess : IDataAccess<TarantoolDataObject, long>
    {
        private readonly Box _box;
        private readonly ISpace _space;
        private readonly IJsonService _jsonService;


        public TarantoolDataAccess(IJsonService jsonService)
        {
            this._jsonService = jsonService;
            _box = Box.Connect("localhost", 3301, "test-user", "tests").GetAwaiter().GetResult();
            this._space = this._box.GetSchema()["reservechannel"];
        }


        public async Task<Unit> Add(TarantoolDataObject @object, CancellationToken token)
        {
            var result = await this._space.Insert(ValueTuple.Create(this._jsonService.Serialize(@object.Data)));
            
            return Unit.Value;
        }


        public Task<TarantoolDataObject> Get(long key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        public Task<List<TarantoolDataObject>> GetAll(long key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        public Task<bool> Delete(long key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }


        public Task<List<TarantoolDataObject>> GetAllByCondition(long key, CancellationToken token)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TarantoolDataObject : DataObjectBase
    {
    }
}
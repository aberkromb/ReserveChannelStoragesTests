using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Telemetry;

namespace ReserveChannelStoragesTests
{
    public class AerospikeDataProvider: IDataAccess<AerospikeDataObject, Key>
    {
        private readonly IDataAccess<AerospikeDataObject, Key> _dataAccess;

        public AerospikeDataProvider(IDataAccess<AerospikeDataObject, Key> dataAccess) => _dataAccess = dataAccess;

        public Task<Unit> Add(AerospikeDataObject @object, CancellationToken token)
        {
            Task<Unit> Func() => _dataAccess.Add(@object, token);

            return TelemetryService.MeasureIt(Func);
        }

        public Task<AerospikeDataObject> Get(Key key, CancellationToken token)
        {
            Task<AerospikeDataObject> Func() => _dataAccess.Get(key, token);
            return TelemetryService.MeasureIt(Func);
        }
    }
}
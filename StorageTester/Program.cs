using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using Newtonsoft.Json;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Telemetry;

namespace StorageTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var users = UserGenerator.CreateRandomUsers(10000);

            var asyncClient = new AsyncClient("localhost", 3000);
            var dataAccess = new AerospikeDataAccess(asyncClient);


            for (var i = 0; i < users.Count; i++)
            {
                var user = users[i];
                var reserveChannel = "reserve_channel";
                var messages = "messages";

                var dataObj = new AerospikeDataObject
                              {
                                  Key = i,
                                  Namespace = reserveChannel,
                                  SetName = messages,
                                  Data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(user))
                              };

                await dataAccess.Add(dataObj, CancellationToken.None);

                var dataObject = await dataAccess.Get(new Key(reserveChannel, messages, i), CancellationToken.None);

//                Console.WriteLine(dataObj.Data.SequenceEqual(dataObject.Data) & dataObj.Key.Equals(dataObject.Key));
            }

            Console.WriteLine(TelemetryService.GetMeasurementsResult());
        }
    }
}
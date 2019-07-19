using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using Newtonsoft.Json;
using ProGaudi.Tarantool.Client;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Telemetry;

namespace StorageTester
{
    class Program
    {
        const string Hostname = "192.168.99.100";

        static async Task Main(string[] args)
        {
            var users = UserGenerator.CreateRandomUsers(10);

            var box = await Box.Connect(Hostname, 3301);

            await AerospikeTester(users);
        }

        private static async Task AerospikeTester(List<UserGenerator.User> users)
        {
//            var asyncClient = new AsyncClient("localhost", 3000);
            var asyncClient = new AsyncClient(Hostname, 3000);
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

                await dataAccess.Delete(new Key(reserveChannel, messages, i), CancellationToken.None);
//                Console.WriteLine(dataObj.Data.SequenceEqual(dataObject.Data) & dataObj.Key.Equals(dataObject.Key));
            }

            var list = await dataAccess.GetAll(new Key("reserve_channel", "messages", 1), CancellationToken.None);

            Console.WriteLine(list.Count);

            foreach (var measurementsResult in TelemetryService.GetMeasurementsResult())
                Console.WriteLine(measurementsResult);
        }
    }
}
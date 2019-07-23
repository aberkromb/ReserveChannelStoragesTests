using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using Newtonsoft.Json;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.PostgresDataAccessImplementation;
using static System.Console;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace StorageTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var messages = Generator.Generator.CreateRandomData(100);

//            File.WriteAllText("json.txt", JsonConvert.SerializeObject(messages));

//            var box = await Box.Connect(Hostname, 3301);

//            await AerospikeTester(users);

            await PostgresTester(messages);
        }


        private static async Task PostgresTester(List<MessageData> messages)
        {
            var dataAccess = new PostgresDataAccess();

            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];

                var dataObj = new PostgresDataObject { DataObject = message };

                await dataAccess.Add(dataObj, CancellationToken.None);

                var savedObject = await dataAccess.Get(dataObj.DataObject.Id, CancellationToken.None);

                await dataAccess.Delete(dataObj.DataObject.Id, CancellationToken.None);
            }

            var list = await dataAccess.GetAll(Guid.Empty, CancellationToken.None);

            WriteLine(list.Count);

            GetMeasurementsResult().ForEach(WriteLine);
        }


        private static async Task AerospikeTester(List<MessageData> messages)
        {
//            var asyncClient = new AsyncClient("localhost", 3000);
            var binarySerializer = new ReserveChannelStoragesTests.BinarySerializers.ZeroFormatter();
            var dataAccess = new AerospikeDataAccess(binarySerializer);

            for (var i = 0; i < messages.Count; i++)
            {
                var messageData = messages[i];
                var ns = "reserve_channel";
                var setName = "messages";

                var dataObj = new AerospikeDataObject
                              {
                                  Key = i,
                                  Namespace = ns,
                                  SetName = setName,
                                  Data = messageData
                              };

                await dataAccess.Add(dataObj, CancellationToken.None);

                var dataObject = await dataAccess.Get(new Key(ns, setName, i), CancellationToken.None);

                await dataAccess.Delete(new Key(ns, setName, i), CancellationToken.None);
//                Console.WriteLine(dataObj.Data.SequenceEqual(dataObject.Data) & dataObj.Key.Equals(dataObject.Key));
            }

            var list = await dataAccess.GetAll(new Key("reserve_channel", "messages", 1), CancellationToken.None);

            WriteLine(list.Count);

            GetMeasurementsResult().ForEach(WriteLine);
        }
    }
}
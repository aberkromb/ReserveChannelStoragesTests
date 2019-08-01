using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using Generator;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Json;
using ReserveChannelStoragesTests.JsonSerializers;
using ReserveChannelStoragesTests.KafkaDataAccessImplementation;
using ReserveChannelStoragesTests.PostgresDataAccessImplementation;
using ReserveChannelStoragesTests.TarantoolDataAccessImplementation;
using static System.Console;
using static ReserveChannelStoragesTests.Telemetry.TelemetryService;

namespace StorageTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var messages = Generator.Generator.CreateRandomData(1000);

//            File.WriteAllText("json.txt", JsonConvert.SerializeObject(messages));


//            await AerospikeTester(messages);
            await TarantoolTester(messages);

//            await KafkaTester(messages);
        }


        private static async Task TarantoolTester(List<MessageData> messages)
        {
            var dataAccess = new TarantoolDataAccess(JsonServiceFactory.GetSerializer("newtonsoft"));

//            for (var i = 0; i < messages.Count; i++)
//            {
//                var message = messages[i];
//
//                var id = await dataAccess.Add(message, CancellationToken.None);
//
//                var savedObject = await dataAccess.Get(id, CancellationToken.None);
//
////                await dataAccess.Delete(id, CancellationToken.None);
//            }

            var list = await dataAccess.GetAll(-1L, CancellationToken.None);
            foreach (var messageData in list)
            {
                await dataAccess.Delete(messageData.Id, CancellationToken.None);
            }

//            WriteLine(list.Count);

            GetMeasurementsResult().ForEach(WriteLine);
        }


        private static async Task KafkaTester(List<MessageData> messages)
        {
            var dataAccess = new KafkaDataAccess(JsonServiceFactory.GetSerializer("newtonsoft"));

            var list = await dataAccess.GetAll(Unit.Value, CancellationToken.None);

            WriteLine(list.Count);
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];

                await dataAccess.Add(message, CancellationToken.None);

//                var savedObject = await dataAccess.Get(dataObj.Data.Id, CancellationToken.None);

//                await dataAccess.Delete(dataObj.Data.Id, CancellationToken.None);
            }

            var batch = await dataAccess.GetBatch(100, CancellationToken.None);


            GetMeasurementsResult().ForEach(WriteLine);
        }


        private static async Task PostgresTester(List<MessageData> messages)
        {
//            var dataAccess = new PostgresDataAccess();
//
//            for (var i = 0; i < messages.Count; i++)
//            {
//                var message = messages[i];
//
//                await dataAccess.Add(message, CancellationToken.None);
//
//                var savedObject = await dataAccess.Get(message.Id, CancellationToken.None);
//
//                await dataAccess.Delete(message.Id, CancellationToken.None);
//            }
//
//            var list = await dataAccess.GetAll(Guid.Empty, CancellationToken.None);
//
//            WriteLine(list.Count);
//
//            GetMeasurementsResult().ForEach(WriteLine);
        }


        private static async Task AerospikeTester(List<MessageData> messages)
        {
//            var asyncClient = new AsyncClient("localhost", 3000);
            var binarySerializer = new Utf8JsonBinaryWrapper();
            var dataAccess = new AerospikeDataAccess(binarySerializer);

            Console.WriteLine(await  dataAccess.DeleteAll());
            
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                var ns = "reserve_channel";
                var setName = "messages";

                await dataAccess.Add(message, CancellationToken.None);

//                Console.WriteLine(dataObj.Data.SequenceEqual(dataObject.Data) & dataObj.Key.Equals(dataObject.Key));
            }

            var batch = await dataAccess.GetBatch(25, CancellationToken.None);

            var list = await dataAccess.GetAll(null, CancellationToken.None);

            WriteLine(list.Count);

            GetMeasurementsResult().ForEach(WriteLine);
        }
    }
}
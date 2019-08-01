using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.Json;
using ReserveChannelStoragesTests.JsonSerializers;
using ReserveChannelStoragesTests.KafkaDataAccessImplementation;
using ReserveChannelStoragesTests.PostgresDataAccessImplementation;
using ReserveChannelStoragesTests.TarantoolDataAccessImplementation;
using ReserveChannelStoragesTests.Telemetry;
using static Generator.Generator;

namespace LoadRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var sw = Stopwatch.StartNew();

            var generateCount = 150_000_000;

            var messages = CreateRandomDataLazy(generateCount);

            try
            {
//                var script = new LoaderBuilder()
//                             .WithJsonSerializer("newtonsoft")
//                             .WithScriptFor("postgres")
//                             .WithScriptConfig(new ScriptConfig { TimeToWrite = TimeSpan.FromMinutes(60), ParallelsCount = 200, GetBatchSize = 1000})
//                             .Build();

//                var script = new LoaderBuilder()
//                             .WithJsonSerializer("newtonsoft")
//                             .WithScriptFor("aerospike")
//                             //в aerospike для чтения бачами используется процент от данных
//                             .WithScriptConfig(new ScriptConfig { TimeToWrite = TimeSpan.FromMinutes(1), ParallelsCount = 20, BatchSize = 5})
//                             .Build();

//                var script = new LoaderBuilder()
//                             .WithJsonSerializer("newtonsoft")
//                             .WithScriptFor("kafka")
//                             .WithScriptConfig(new ScriptConfig { TimeToWrite = TimeSpan.FromMinutes(1), WriteParallelsCount = 1, GetBatchSize = 1000})
//                             .Build();

                var script = new LoaderBuilder()
                             .WithJsonSerializer("newtonsoft")
                             .WithScriptFor("tarantool")
                             .WithScriptConfig(new ScriptConfig { TimeToWrite = TimeSpan.FromMinutes(60), WriteParallelsCount = 200, GetBatchSize = 1000})
                             .Build();

                await script.Run(messages, CancellationToken.None);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var measurementsResult = TelemetryService.GetMeasurementsResult();
            measurementsResult.ForEach(Console.WriteLine);
            TelemetryService.DumpRawData();

            sw.Stop();

            Console.WriteLine($"End in {sw.Elapsed}");
            Console.ReadLine();
        }
    }


    public class LoaderBuilder
    {
        private string storageName = null;
        private string jsonSerizlizerName = null;


        public LoaderBuilder WithScriptFor(string storage)
        {
            this.storageName = storage;
            return this;
        }


        public LoaderBuilder WithJsonSerializer(string name)
        {
            jsonSerizlizerName = name;
            return this;
        }


        private ScriptConfig scriptConfig;


        public LoaderBuilder WithScriptConfig(ScriptConfig config)
        {
            this.scriptConfig = config;
            return this;
        }


        public IScript Build()
        {
            var config = this.scriptConfig ?? new ScriptConfig();

            var script = this.CreateScript(this.storageName, this.jsonSerizlizerName, config);

            return script;
        }


        private IScript CreateScript(string name, string jsonSerializerName, ScriptConfig config)
        {
            switch (name)
            {
                case "aerospike":
                    var aerospike = new AerospikeDataAccess(new Utf8JsonBinaryWrapper());
                    return new AerospikeSimpleWriteRead(aerospike, config);
                case "postgres":
                    var postgres = new PostgresDataAccess();
                    return new PostgresSimpleWriteReadScript(postgres, config);
                case "kafka":
                    var kafka = new KafkaDataAccess(JsonServiceFactory.GetSerializer(jsonSerializerName ?? "newtonsoft"));
                    return new KafkaSimpleWriteRead(kafka, config);
                case "tarantool":
                    var tarantool = new TarantoolDataAccess(JsonServiceFactory.GetSerializer(jsonSerializerName ?? "newtonsoft"));
                    return new TarantoolSimpleWriteRead(tarantool, config);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
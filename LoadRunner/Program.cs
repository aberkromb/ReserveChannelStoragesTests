using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.BinarySerializers;
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

            var generateCount = 140_000_000;

            var messages = CreateRandomDataLazy(generateCount);

            try
            {
                var script = new LoaderBuilder()
                             .WithJsonSerializer("newtonsoft")
                             .WithScriptFor("postgres")
                             .WithScriptConfig(new ScriptConfig { TimeToWrite = TimeSpan.FromMinutes(10), ParallelsCount = 20, BatchSize = 10000})
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
                    var aerospike = new AerospikeDataAccess(new ZeroFormatterWrapper());
                    return new AerospikeSimpleWriteRead(aerospike, config);
                case "postgres":
                    var postgres = new PostgresDataAccess();
                    return new PostgresSimpleWriteReadScript(postgres, config);
                case "kafka":
                    var kafka = new KafkaDataAccess(JsonServiceFactory.GetSerializer(jsonSerializerName ?? "newtonsoft"));
                    return new KafkaSimpleWriteRead();
                case "tarantool":
                    var tarantool = new TarantoolDataAccess(JsonServiceFactory.GetSerializer(jsonSerializerName ?? "newtonsoft"));
                    return new TarantoolSimpleWriteRead();
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
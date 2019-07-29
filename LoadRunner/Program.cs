using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Generator;
using ReserveChannelStoragesTests;
using ReserveChannelStoragesTests.AerospikeDataAccessImplementation;
using ReserveChannelStoragesTests.BinarySerializers;
using ReserveChannelStoragesTests.JsonSerializers;
using ReserveChannelStoragesTests.KafkaDataAccessImplementation;
using ReserveChannelStoragesTests.PostgresDataAccessImplementation;
using ReserveChannelStoragesTests.TarantoolDataAccessImplementation;
using ReserveChannelStoragesTests.Telemetry;
using Rocks.Dataflow;
using Rocks.Dataflow.Fluent;

namespace LoadRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var sw = Stopwatch.StartNew();
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(120));

            var generateCount = 1_000_000;

            var messages = Generator.Generator.CreateRandomDataLazy(generateCount);

            try
            {
                var dataflow = new LoaderBuilder()
                               .WithJsonSerializer("newtonsoft")
                               .WithScriptFor("postgres")
                               .WithScriptConfig(new ScriptConfig() { TimeToWrite = TimeSpan.FromMinutes(1) })
                               .WithLoaderConfig(new LoaderConfig { ParallelsCount = 1 })
                               .Build(cts.Token);

                await dataflow.Run(messages, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            TelemetryService.GetMeasurementsResult().ForEach(Console.WriteLine);

            sw.Stop();

            Console.WriteLine($"End in {sw.Elapsed}");
        }
    }

    public class LoaderBuilder
    {
        private IScript script;

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


        private LoaderConfig loaderConfig;


        public LoaderBuilder WithLoaderConfig(LoaderConfig config)
        {
            this.loaderConfig = config;
            return this;
        }


        private ScriptConfig scriptConfig;


        public LoaderBuilder WithScriptConfig(ScriptConfig config)
        {
            this.scriptConfig = config;
            return this;
        }


        public IScript Build(CancellationToken cancellationToken)
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
                    return new AerospikeSimpleWriteRead();
                case "postgres":
                    var postgres = new PostgresDataAccess();
                    return new PostgresSimpleWriteRead(postgres, config);
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

    public class LoaderConfig
    {
        public int ParallelsCount { get; set; } = 10;
    }
}
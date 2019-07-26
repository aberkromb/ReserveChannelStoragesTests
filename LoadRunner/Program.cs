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
                               .WithScriptConfig(new ScriptConfig())
                               .WithLoaderConfig(new LoaderConfig { ParallelsCount = 1 })
                               .Build(cts.Token);

                await dataflow.ProcessAsync(messages, cts.Token);
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


        private IScript CreateScript(string name, string jsonSerializerName, ScriptConfig scriptConfig)
        {
            switch (name)
            {
                case "aerospike":
                    var aerospike = new AerospikeDataAccess(new ZeroFormatterWrapper());
                    return new AerospikeSimpleWriteRead();
                case "postgres":
                    var postgres = new PostgresDataAccess();
                    return new PostgresSimpleWriteRead(postgres, scriptConfig);
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


        public Dataflow<MessageData> Build(CancellationToken cancellationToken)
        {
            var lConfig = this.loaderConfig ?? new LoaderConfig();
            var sConfig = this.scriptConfig ?? new ScriptConfig();

            var script = this.CreateScript(this.storageName, this.jsonSerizlizerName, sConfig);

            var dataflow = DataflowFluent
                           .ReceiveDataOfType<MessageData>()
                           .ProcessAsync(data => script.Write(data, cancellationToken))
                           .WithMaxDegreeOfParallelism(lConfig.ParallelsCount)
                           .ProcessAsync(data => script.Read(cancellationToken))
                           .ProcessAsync(data => script.AmountRemaining(cancellationToken))
                           .WithMaxDegreeOfParallelism(1)
                           .WithDefaultExceptionLogger((exception, o) => Console.WriteLine(exception))
                           .Action(x => { })
                           .CreateDataflow();

            
            return dataflow;
        }
    }

    public class LoaderConfig
    {
        public int ParallelsCount { get; set; } = 10;
    }
}
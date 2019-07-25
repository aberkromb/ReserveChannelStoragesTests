using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Generator;
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
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            var messages = Generator.Generator.CreateRandomDataLazy(10);

            var dataflow = LoaderBuilder.CreateDataflow(new Configuration { GenerateCount = 1 }, cts.Token);

            await dataflow.ProcessAsync(messages, cts.Token);

            sw.Stop();
            Console.WriteLine($"End in {sw.Elapsed}");
        }
    }

    public class LoaderBuilder
    {
        public static Dataflow<MessageData> CreateDataflow(Configuration parameters, CancellationToken cancellationToken)
        {
            var dataflow = DataflowFluent
                           .ReceiveDataOfType<MessageData>()
                           .ProcessAsync(data => Task.FromResult(data))
                           .WithMaxDegreeOfParallelism(parameters.ParallelsCount)
                           .ProcessAsync(data => Write(data, cancellationToken))
                           .Action(x => TelemetryService.GetMeasurementsResult().ForEach(Console.WriteLine))
                           .CreateDataflow();

            return dataflow;
        }


        private static Task Write(MessageData message, CancellationToken token)
        {
        }
    }

    public class Configuration
    {
        public int GenerateCount { get; set; }
        public int ParallelsCount { get; set; }
    }
}
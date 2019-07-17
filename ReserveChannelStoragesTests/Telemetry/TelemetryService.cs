using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ReserveChannelStoragesTests.Telemetry
{
    public static class TelemetryService
    {
        private static ConcurrentBag<(string, long)> measurements = new ConcurrentBag<(string, long)>();


        public static async Task<TOut> MeasureIt<TOut>(Func<Task<TOut>> func, [CallerMemberName] string caller = "")
        {
            var sw = Stopwatch.StartNew();

            var functionResult = await func();

            sw.Stop();
            measurements.Add((caller, sw.ElapsedMilliseconds));

            return functionResult;
        }

        private static Dictionary<string, List<long>> ToDictionary()
        {
            var dict = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (callerName, time) in measurements)
            {
                if (dict.ContainsKey(callerName))
                    dict[callerName].Add(time);
                else
                    dict[callerName] = new List<long> {time};
            }

            return dict;
        }

        public static List<MeasurementsResult> GetMeasurementsResult()
        {
            var dict = ToDictionary();

            var results = new List<MeasurementsResult>(dict.Keys.Count);

            foreach (var (opName, measurements) in dict)
            {
                results.Add(ToMeasurementsResult(opName, measurements));
            }

            return results;
        }

        private static MeasurementsResult ToMeasurementsResult(string operationName, List<long> measurements) =>
            new MeasurementsResult
            {
                OperationName = operationName,
                Average = measurements.Average(),
                Median = measurements.Median(),
                Max = measurements.Max(),
                Min = measurements.Min(),
                Count = measurements.Count,
                TotalElapsed = measurements.Aggregate(TimeSpan.Zero,
                    (span, milliseconds) => span.Add(TimeSpan.FromMilliseconds(milliseconds)))
            };
    }

    public class MeasurementsResult
    {
        public double Average { get; set; }
        public double Median { get; set; }
        public long Max { get; set; }
        public long Min { get; set; }

        public int Count { get; set; }
        public TimeSpan TotalElapsed { get; set; }
        public string OperationName { get; set; }


        public override string ToString() =>
            $"OperationName: {OperationName} Average: {Average}, Median: {Median}, Max: {Max}, Min: {Min}, Count: {Count}, Total: {TotalElapsed}";
    }

    public static class TelemetryExtensions
    {
        public static double Median(this IEnumerable<long> numbers)
        {
            int numberCount = numbers.Count();
            int halfIndex = numbers.Count() / 2;
            var sortedNumbers = numbers.OrderBy(n => n);
            double median;
            if (numberCount % 2 == 0)
            {
                var firstElement = sortedNumbers.ElementAt(halfIndex);
                var secondElement = sortedNumbers.ElementAt(halfIndex - 1);
                median = (firstElement + secondElement) / 2;
            }
            else
            {
                median = sortedNumbers.ElementAt(halfIndex);
            }

            return median;
        }
    }
}
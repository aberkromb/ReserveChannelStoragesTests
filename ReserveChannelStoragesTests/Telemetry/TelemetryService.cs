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
                    dict[callerName] = new List<long> { time };
            }

            return dict;
        }


        public static List<MeasurementsResult> GetMeasurementsResult()
        {
            var dict = ToDictionary();

            var results = new List<MeasurementsResult>(dict.Keys.Count);

            foreach (var (operationName, measurements) in dict)
            {
                results.Add(ToMeasurementsResult(operationName, measurements));
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
                Percetile90 = measurements.Percentile(.90),
                Percetile99 = measurements.Percentile(.99),
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
        public double Percetile90 { get; set; }
        public double Percetile99 { get; set; }


        public override string ToString() =>
            $"OperationName: {OperationName}, \n Average: {Average},\n Median: {Median},\n Max: {Max},\n Min: {Min},\n Percetile90: {this.Percetile90},\n Percetile99: {this.Percetile99},\n Count: {Count},\n Total: {TotalElapsed}";
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


        public static double Percentile(this IList<long> sequence, double excelPercentile)
        {
            var arr = sequence.ToArray();
            Array.Sort(arr);
            var N = arr.Length;
            var n = (N - 1) * excelPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return arr[0];
            else if (n == N) return arr[N - 1];
            else
            {
                int k = (int) n;
                double d = n - k;
                return arr[k - 1] + d * (arr[k] - arr[k - 1]);
            }
        }
    }
}
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


        public static MeasurementsResult GetMeasurementsResult() =>
            new MeasurementsResult
            {
                Average = measurements.Average(tuple => tuple.Item2),
                Median = measurements.Median(tuple => tuple.Item2),
                Max = measurements.Max(tuple => tuple.Item2),
                Min = measurements.Min(tuple => tuple.Item2),
                Count = measurements.Count
            };
    }

    public class MeasurementsResult
    {
        public double Average { get; set; }
        public double Median { get; set; }
        public long Max { get; set; }
        public long Min { get; set; }

        public int Count { get; set; }


        public override string ToString() =>
            $"Average: {Average}, Median: {Median}, Max: {Max}, Min: {Min}, Count: {Count}";
    }

    public static class TelemetryExtensions
    {
        public static double Median<T>(this IEnumerable<T> list, Func<T, long> selector)
        {
            var numbers = list.Select(selector);
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
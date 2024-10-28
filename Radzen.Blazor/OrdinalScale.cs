using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class OrdinalScale : LinearScale
    {
        public IList<object> Data { get; set; }

        public override object Value(double value)
        {
            return Data.ElementAtOrDefault(Convert.ToInt32(value));
        }

        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var count = Data.Count - 1;
            var ticks = Math.Min(Math.Ceiling(Math.Abs((Output.End - Padding) - (Output.Start + Padding)) / distance), count);
            var step = (count) / ticks;

            var start = - step;
            var end = count + step;

            Console.WriteLine($"Ticks: {ticks} Start: {start} End: {end} Step: {step}");

            return (start, end, step);
        }
    }
}
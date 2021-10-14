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
            var start = -1;
            var end = Data.Count;
            var step = 1;

            return (start, end, step);
        }
    }
}
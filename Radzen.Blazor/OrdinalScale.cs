using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class OrdinalScale : LinearScale
    {
        public IList<object>? Data { get; set; }

        public override object Value(double value)
        {
            if (Data == null)
            {
                return default!;
            }

            var index = Convert.ToInt32(value);
            if (index < 0 || index >= Data.Count)
            {
                return default!;
            }

            return Data[index];
        }

        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var start = -1;
            var end = Data?.Count ?? 0;
            var step = 1;

            return (start, end, step);
        }
    }
}
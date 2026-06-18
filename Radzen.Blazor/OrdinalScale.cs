using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class OrdinalScale : LinearScale
    {
        public IList<object>? Data { get; set; }

        /// <summary>
        /// Controls the space reserved at each end of the axis. <see cref="TickPlacement.Between" />
        /// (the default) centers each category in its band, reserving half a band at each end;
        /// <see cref="TickPlacement.On" /> places categories on the ticks, flush to the plot edges.
        /// </summary>
        public TickPlacement Placement { get; set; } = TickPlacement.Between;

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
            var count = Data?.Count ?? 0;

            // A single (or no) category cannot span a range edge-to-edge without collapsing the input
            // to zero width, so keep a symmetric half-band domain in that case.
            if (Placement == TickPlacement.On && count >= 2)
            {
                // Flush: categories sit on the ticks, first/last at the plot edges.
                return (0, count - 1, 1);
            }

            // Between: half a band of slack on each side so categories are centered in their band.
            return (-0.5, count - 0.5, 1);
        }

        public override IEnumerable<double> TickValues(int distance)
        {
            // Ticks define the domain extent (which may include half-band slack); labels and gridlines
            // always land on the integer category indices regardless of placement.
            var count = Data?.Count ?? 0;

            for (var i = 0; i < count; i++)
            {
                yield return i;
            }
        }
    }
}
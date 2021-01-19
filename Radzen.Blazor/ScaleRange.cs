using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    public class ScaleRange
    {
        public double Start { get; set; } = Double.PositiveInfinity;
        public double End { get; set; } = Double.NegativeInfinity;

        public static ScaleRange From<T>(IEnumerable<T> data, Func<T, double> selector)
        {
            var start = data.Min(selector);
            var end = data.Max(selector);

            return new ScaleRange() { Start = start, End = end };
        }

        public double Clamp(double value)
        {
            if (value > End)
            {
                return End;
            }

            if (value < Start)
            {
                return Start;
            }

            return value;
        }

        public double Size
        {
            get
            {
                return Math.Abs(End - Start);
            }
        }

        public double Mid
        {
            get
            {
                return Size / 2;
            }
        }

        public void MergeWidth(ScaleRange range)
        {
          Start = Math.Min(Start, range.Start);
          End = Math.Max(End, range.End);
        }

        public bool IsEqualTo(ScaleRange range)
        {
            return Start == range.Start && End == range.End;
        }
    }
}
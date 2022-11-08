using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    internal class LinearScale : ScaleBase
    {
        public override object Value(double value)
        {
            return value;
        }

        public override string FormatTick(string format, object value)
        {
            if (value == null)
            {
                return String.Empty;
            }

            if (String.IsNullOrEmpty(format))
            {
                return value.ToString();
            }

            return string.Format(format, value);
        }

        public override double Scale(double value, bool padding)
        {
            var outputSize = Output.End - Output.Start;

            if (padding)
            {
                outputSize -= Padding * 2;
            }

            var inputSize = Input.End - Input.Start;
            var result = (value - Input.Start) / inputSize * outputSize;

            if (outputSize < 0)
            {
                result -= outputSize;
            }

            if (padding)
            {
                result += Padding;
            }

            return result;
        }

        protected virtual double CalculateTickCount(int distance)
        {
            return Math.Ceiling(Math.Abs(Output.End - Output.Start) / distance);
        }

        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var ticks = CalculateTickCount(distance);
            var start = Input.Start;
            var end = Input.End;

            if (start == end)
            {
                start = 0;
                end += NiceNumber(end / ticks, false);
            }

            if (Round && end < 0)
            {
                end = 0;
                start += NiceNumber(start / ticks, false);
            }

            var range = end - start;

            if (Round)
            {
                range = NiceNumber(end - start, false);
            }

            var step = Round ? NiceNumber(range / ticks, true) : Math.Ceiling(range / ticks);

            if (Step != null)
            {
                if (Step is IConvertible)
                {
                    step = Convert.ToDouble(Step);

                    if (step <= 0)
                    {
                        throw new ArgumentOutOfRangeException("Step must be greater than zero");
                    }
                }
            }

            if (Round)
            {
                start = Math.Floor(start / step) * step;
                end = Math.Ceiling(end / step) * step;
            }

            if (!Double.IsFinite(Input.Start) && !Double.IsFinite(Input.End))
            {
                Input.Start = start = 0;
                Input.End = end = 2;
                Step = step = 1;
                Round = false;
            }

            if (!Double.IsFinite(start) && !Double.IsFinite(end))
            {
                Input.Start = start = 0;
                Input.End = end = 2;
                Step = step = 1;
                Round = false;
            }

            return (start, end, step);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class LinearScale.
    /// Implements the <see cref="Radzen.Blazor.ScaleBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ScaleBase" />
    internal class LinearScale : ScaleBase
    {
        /// <summary>
        /// Values the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Object.</returns>
        public override object Value(double value)
        {
            return value;
        }

        /// <summary>
        /// Formats the tick.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Scales the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="padding">if set to <c>true</c> [padding].</param>
        /// <returns>System.Double.</returns>
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

        /// <summary>
        /// Calculates the tick count.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>System.Double.</returns>
        protected virtual double CalculateTickCount(int distance)
        {
            return Math.Ceiling(Math.Abs(Output.End - Output.Start) / distance);
        }

        /// <summary>
        /// Tickses the specified distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double, System.Double&gt;.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Step must be greater than zero</exception>
        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var ticks = CalculateTickCount(distance);
            var start = Input.Start;
            var end = Input.End;

            if (start == end)
            {
                start = 0;
                end = end + NiceNumber(end/ticks, false);
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

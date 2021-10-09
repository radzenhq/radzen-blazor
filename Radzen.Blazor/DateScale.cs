using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class DateScale.
    /// Implements the <see cref="Radzen.Blazor.LinearScale" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.LinearScale" />
    internal class DateScale : LinearScale
    {
        /// <summary>
        /// Tickses the specified distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double, System.Double&gt;.</returns>
        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var start = Input.Start;

            var end = Input.End;

            var step = Math.Abs(end - start) / CalculateTickCount(distance);

            if (Step != null)
            {
                if (Step is TimeSpan timeSpan)
                {
                    step = timeSpan.Ticks;
                }
            }

            if (!Double.IsFinite(start) && !Double.IsFinite(end))
            {
                start = DateTime.Now.Date.AddDays(-1).Ticks;
                end = DateTime.Now.Date.AddDays(1).Ticks;
                Step = TimeSpan.FromDays(1);
                step = TimeSpan.FromDays(1).Ticks;
            }

            if (start == end)
            {
                start = FromTicks(start).Date.AddDays(-1).Ticks;
                end = FromTicks(end).Date.AddDays(1).Ticks;
                Step = TimeSpan.FromDays(1);
                step = TimeSpan.FromDays(1).Ticks;
            }

            return (start, end, step);
        }

        /// <summary>
        /// Values the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Object.</returns>
        public override object Value(double value)
        {
            return FromTicks(value);
        }

        /// <summary>
        /// Resizes the specified minimum.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public override void Resize(object min, object max)
        {
            if (min != null)
            {
                var minDate = Convert.ToDateTime(min);
                Input.Start = minDate.Ticks;
                Round = false;
            }

            if (max != null)
            {
                var maxDate = Convert.ToDateTime(max);
                Input.End = maxDate.Ticks;
                Round = false;
            }
        }

        /// <summary>
        /// Froms the ticks.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>DateTime.</returns>
        private DateTime FromTicks(double value)
        {
            return new DateTime(Convert.ToInt64(value));
        }

        /// <summary>
        /// Formats the tick.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        public override string FormatTick(string format, object value)
        {
            if (string.IsNullOrEmpty(format))
            {
                return ((DateTime)value).ToShortDateString();
            }

            return base.FormatTick(format, value);
        }

        /// <summary>
        /// Fits the specified distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        public override void Fit(int distance)
        {
            var ticks = Ticks(distance);

            Input.MergeWidth(new ScaleRange { Start = ticks.Start, End = ticks.End });

            Round = false;
            Step = TimeSpan.FromTicks(Convert.ToInt64(ticks.Step));
        }
    }
}

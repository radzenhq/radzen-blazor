using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents a range of values.
    /// </summary>
    public class ScaleRange
    {
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        /// <value>The start.</value>
        public double Start { get; set; } = double.PositiveInfinity;
        /// <summary>
        /// Gets or sets the end.
        /// </summary>
        /// <value>The end.</value>
        public double End { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// Creates a <c>ScaleRange</c> from the specified data.
        /// </summary>
        /// <typeparam name="T">Type of the data item.</typeparam>
        /// <param name="data">The data.</param>
        /// <param name="selector">The selector.</param>
        public static ScaleRange From<T>(IEnumerable<T> data, Func<T, double> selector)
        {
            var start = data.Min(selector);
            var end = data.Max(selector);

            return new ScaleRange() { Start = start, End = end };
        }

        /// <summary>
        /// Clamps the specified value within the current <see cref="Start" /> and <see cref="End" />.
        /// </summary>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        public double Size
        {
            get
            {
                return Math.Abs(End - Start);
            }
        }

        /// <summary>
        /// Gets the mid.
        /// </summary>
        /// <value>The mid.</value>
        public double Mid
        {
            get
            {
                return Size / 2;
            }
        }

        /// <summary>
        /// Merges the width.
        /// </summary>
        /// <param name="range">The range.</param>
        public void MergeWidth(ScaleRange range)
        {
          Start = Math.Min(Start, range.Start);
          End = Math.Max(End, range.End);
        }

        /// <summary>
        /// Determines whether the current range is equal to the specified one.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <returns><c>true</c> if the ranges are equal; otherwise, <c>false</c>.</returns>
        public bool IsEqualTo(ScaleRange range)
        {
            return Start == range.Start && End == range.End;
        }
    }
}
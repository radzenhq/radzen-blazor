using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class OrdinalScale.
    /// Implements the <see cref="Radzen.Blazor.LinearScale" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.LinearScale" />
    internal class OrdinalScale : LinearScale
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public IList<object> Data { get; set; }

        /// <summary>
        /// Values the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Object.</returns>
        public override object Value(double value)
        {
            return Data.ElementAtOrDefault(Convert.ToInt32(value));
        }

        /// <summary>
        /// Tickses the specified distance.
        /// </summary>
        /// <param name="distance">The distance.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double, System.Double&gt;.</returns>
        public override (double Start, double End, double Step) Ticks(int distance)
        {
            var start = -1;
            var end = Data.Count;
            var step = 1;

            return (start, end, step);
        }
    }
}
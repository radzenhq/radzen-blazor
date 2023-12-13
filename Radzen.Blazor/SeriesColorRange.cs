using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents a Color Range object for passing to PickColor />
    /// </summary>
    public class SeriesColorRange
    {
        /// <summary>
        /// Gets or sets the minimum value of the range. Null will use default double.Min
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value of the range. Null will use default double.Max
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get; set; }

        /// <summary>
        /// Gets or sets the color of the range.
        /// </summary>
        /// <value>The color.</value>
        public string Color { get; set; }
    }
}
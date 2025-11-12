using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class LineGenerator.
    /// Implements the <see cref="Radzen.Blazor.Rendering.IPathGenerator" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.Rendering.IPathGenerator" />
    public class LineGenerator : IPathGenerator
    {
        /// <summary>
        /// Pathes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>System.String.</returns>
        public string Path(IEnumerable<Point> data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var path = new StringBuilder();
            var start = true;

            foreach (var item in data)
            {
                var x = item.X;
                var y = item.Y;

                if (start)
                {
                    start = false;
                }
                else
                {
                    path.Append("L ");
                }

                path.Append(CultureInfo.InvariantCulture, $"{x.ToInvariantString()} {y.ToInvariantString()} ");
            }

            return path.ToString();
        }
    }
}

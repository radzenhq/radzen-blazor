using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class StepGenerator.
    /// Implements the <see cref="Radzen.Blazor.Rendering.IPathGenerator" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.Rendering.IPathGenerator" />
    public class StepGenerator : IPathGenerator
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
            var needsMoveTo = true;

            foreach (var point in data)
            {
                if (double.IsNaN(point.X) || double.IsNaN(point.Y))
                {
                    needsMoveTo = true;
                    continue;
                }

                if (needsMoveTo)
                {
                    if (path.Length > 0)
                    {
                        path.Append(" M ");
                    }
                    path.Append(CultureInfo.InvariantCulture, $"{point.X.ToInvariantString()} {point.Y.ToInvariantString()}");
                    needsMoveTo = false;
                    continue;
                }

                path.Append(CultureInfo.InvariantCulture, $" H {point.X.ToInvariantString()}");
                path.Append(CultureInfo.InvariantCulture, $" V {point.Y.ToInvariantString()}");
            }

            return path.ToString();
        }
    }
}

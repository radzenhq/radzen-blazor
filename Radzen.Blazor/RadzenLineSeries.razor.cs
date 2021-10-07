using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenLineSeries.
    /// Implements the <see cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.CartesianSeries{TItem}" />
    public partial class RadzenLineSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the type of the line.
        /// </summary>
        /// <value>The type of the line.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenLineSeries{TItem}"/> is smooth.
        /// </summary>
        /// <value><c>true</c> if smooth; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Smooth { get; set; }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        public override string Color
        {
            get
            {
                return Stroke;
            }
        }
        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
        public override bool Contains(double x, double y, double tolerance)
        {
            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);

            var points = Items.Select(item => new Point { X = category(item), Y = value(item) }).ToArray();

            if (points.Any())
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    var start = points[i];
                    var end = points[i + 1];

                    var polygon = new[] {
                    new Point { X = start.X, Y = start.Y - tolerance },
                    new Point { X = end.X, Y = end.Y - tolerance },
                    new Point { X = end.X, Y = end.Y + tolerance },
                    new Point { X = start.X, Y = start.Y + tolerance },
                };

                    if (InsidePolygon(new Point { X = x, Y = y }, polygon))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
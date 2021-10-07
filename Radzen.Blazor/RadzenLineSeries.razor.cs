using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    public partial class RadzenLineSeries<TItem> : CartesianSeries<TItem>
    {
        [Parameter]
        public string Stroke { get; set; }

        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        [Parameter]
        public LineType LineType { get; set; }

        [Parameter]
        public bool Smooth { get; set; }

        public override string Color
        {
            get
            {
                return Stroke;
            }
        }
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
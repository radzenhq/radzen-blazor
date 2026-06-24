using System;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays a shaded reference band between two fixed values over a chart series. Use it to highlight acceptable ranges, thresholds or zones on the value axis.
    /// </summary>
    /// <example>
    /// <code>
    ///   &lt;RadzenChart&gt;
    ///       &lt;RadzenLineSeries Data=@revenue CategoryProperty="Quarter" ValueProperty="Revenue"&gt;
    ///          &lt;RadzenSeriesReferenceBand From="240000" To="280000" Title="Acceptable range" /&gt;
    ///       &lt;/RadzenLineSeries&gt;
    ///   &lt;/RadzenChart&gt;
    ///   @code {
    ///       class DataItem
    ///       {
    ///           public string Quarter { get; set; }
    ///           public double Revenue { get; set; }
    ///       }
    ///       DataItem[] revenue = new DataItem[]
    ///       {
    ///           new DataItem { Quarter = "Q1", Revenue = 234000 },
    ///           new DataItem { Quarter = "Q2", Revenue = 284000 },
    ///           new DataItem { Quarter = "Q3", Revenue = 274000 },
    ///           new DataItem { Quarter = "Q4", Revenue = 294000 }
    ///       };
    ///   }
    /// </code>
    /// </example>
    public class RadzenSeriesReferenceBand : RadzenGridLines, IChartSeriesOverlay, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RadzenSeriesReferenceBand"/> class.
        /// </summary>
        public RadzenSeriesReferenceBand()
        {
            Visible = true;
        }

        /// <summary>
        /// Specifies the start value of the reference band on the value axis.
        /// </summary>
        [Parameter]
        public double From { get; set; }

        /// <summary>
        /// Specifies the end value of the reference band on the value axis.
        /// </summary>
        [Parameter]
        public double To { get; set; }

        /// <summary>
        /// Specifies the title of the reference band. Displayed as label in the tooltip. Set to <c>"Reference"</c> by default.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// Specifies the fill color of the reference band. Set to <c>var(--rz-series-color)</c> by default - the color of the series the band belongs to.
        /// </summary>
        [Parameter]
        public string Fill { get; set; } = "var(--rz-series-color)";

        /// <summary>
        /// Specifies the fill opacity of the reference band. Set to <c>0.1</c> by default.
        /// </summary>
        [Parameter]
        public double FillOpacity { get; set; } = 0.1;

        /// <summary>
        /// Specifies the template displayed in the tooltip of the reference band.
        /// </summary>
        [Parameter]
        public RenderFragment<(double From, double To)>? TooltipTemplate { get; set; }

        IChartSeries? series;

        /// <summary>
        /// The series this reference band applies to. Set by the parent series via a cascading parameter.
        /// </summary>
        [CascadingParameter]
        protected IChartSeries? Series
        {
            get
            {
                return series;
            }
            set
            {
                if (value == null)
                {
                    series = null;
                    return;
                }
                if (value.CoordinateSystem != CoordinateSystem.Cartesian)
                {
                    throw new ArgumentException($"Series must use Cartesian coordinate system");
                }
                series = value;
                if (!series.Overlays.Contains(this))
                {
                    series.Overlays.Add(this);
                }
            }
        }

        /// <summary>
        /// Gets the label displayed in the tooltip of the reference band.
        /// </summary>
        protected virtual string Name => Title ?? "Reference";

        double Start => Math.Min(From, To);

        double End => Math.Max(From, To);

        /// <inheritdoc />
        public RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale)
        {
            ArgumentNullException.ThrowIfNull(categoryScale);
            ArgumentNullException.ThrowIfNull(valueScale);

            if (Chart == null)
            {
                return null!;
            }

            double x, y, width, height;

            if (Chart.ShouldInvertAxes())
            {
                var a = categoryScale.Scale(Start);
                var b = categoryScale.Scale(End);
                var left = Math.Max(0, Math.Min(a, b));
                var right = Math.Min(Chart.CategoryScale.OutputSize, Math.Max(a, b));

                if (right <= left)
                {
                    return null!;
                }

                x = left;
                width = right - left;
                y = 0;
                height = Chart.ValueScale.OutputSize;
            }
            else
            {
                var a = valueScale.Scale(Start);
                var b = valueScale.Scale(End);
                var top = Math.Max(0, Math.Min(a, b));
                var bottom = Math.Min(Chart.ValueScale.OutputSize, Math.Max(a, b));

                if (bottom <= top)
                {
                    return null!;
                }

                x = 0;
                width = Chart.CategoryScale.OutputSize;
                y = top;
                height = bottom - top;
            }

            var index = series != null ? Chart.Series.IndexOf(series) : -1;

            return builder =>
            {
                builder.OpenElement(0, "g");
                builder.AddAttribute(1, "class", index >= 0 ? $"rz-series-{index}" : null);

                builder.OpenElement(2, "rect");
                builder.SetKey($"{x.ToInvariantString()}-{y.ToInvariantString()}-{width.ToInvariantString()}-{height.ToInvariantString()}-{index}");
                builder.AddAttribute(3, "x", x.ToInvariantString());
                builder.AddAttribute(4, "y", y.ToInvariantString());
                builder.AddAttribute(5, "width", width.ToInvariantString());
                builder.AddAttribute(6, "height", height.ToInvariantString());
                builder.AddAttribute(7, "style", $"fill: {Fill}; fill-opacity: {FillOpacity.ToInvariantString()}; stroke: none;");
                builder.CloseElement();

                if (!string.IsNullOrEmpty(Stroke))
                {
                    string fromPath, toPath;

                    if (Chart.ShouldInvertAxes())
                    {
                        var bottom = Chart.ValueScale.OutputSize;
                        fromPath = $"M{x.ToInvariantString()} 0 L{x.ToInvariantString()} {bottom.ToInvariantString()}";
                        toPath = $"M{(x + width).ToInvariantString()} 0 L{(x + width).ToInvariantString()} {bottom.ToInvariantString()}";
                    }
                    else
                    {
                        var right = Chart.CategoryScale.OutputSize;
                        fromPath = $"M0 {y.ToInvariantString()} L{right.ToInvariantString()} {y.ToInvariantString()}";
                        toPath = $"M0 {(y + height).ToInvariantString()} L{right.ToInvariantString()} {(y + height).ToInvariantString()}";
                    }

                    builder.OpenComponent<Path>(8);
                    builder.AddAttribute(9, nameof(Path.D), fromPath);
                    builder.AddAttribute(10, nameof(Path.Stroke), Stroke);
                    builder.AddAttribute(11, nameof(Path.StrokeWidth), StrokeWidth);
                    builder.AddAttribute(12, nameof(Path.LineType), LineType);
                    builder.CloseComponent();

                    builder.OpenComponent<Path>(13);
                    builder.AddAttribute(14, nameof(Path.D), toPath);
                    builder.AddAttribute(15, nameof(Path.Stroke), Stroke);
                    builder.AddAttribute(16, nameof(Path.StrokeWidth), StrokeWidth);
                    builder.AddAttribute(17, nameof(Path.LineType), LineType);
                    builder.CloseComponent();
                }

                builder.CloseElement();
            };
        }

        /// <inheritdoc />
        public bool Contains(double mouseX, double mouseY, int tolerance)
        {
            if (double.IsNaN(From) || double.IsNaN(To) || Chart == null)
            {
                return false;
            }

            if (Chart.ShouldInvertAxes())
            {
                var a = Chart.CategoryScale.Scale(Start);
                var b = Chart.CategoryScale.Scale(End);
                return (mouseY >= -tolerance && mouseY <= Chart.ValueScale.OutputSize + tolerance) &&
                       (mouseX >= Math.Min(a, b) - tolerance && mouseX <= Math.Max(a, b) + tolerance);
            }
            else
            {
                var a = Chart.ValueScale.Scale(Start);
                var b = Chart.ValueScale.Scale(End);
                return (mouseX >= -tolerance && mouseX <= Chart.CategoryScale.OutputSize + tolerance) &&
                       (mouseY >= Math.Min(a, b) - tolerance && mouseY <= Math.Max(a, b) + tolerance);
            }
        }

        /// <inheritdoc />
        public RenderFragment RenderTooltip(double mouseX, double mouseY)
        {
            if (Chart == null)
            {
                return null!;
            }

            string from, to;

            if (Chart.ShouldInvertAxes())
            {
                from = Chart.ValueAxis.Format(Chart.CategoryScale, From);
                to = Chart.ValueAxis.Format(Chart.CategoryScale, To);
            }
            else
            {
                from = Chart.ValueAxis.Format(Chart.ValueScale, From);
                to = Chart.ValueAxis.Format(Chart.ValueScale, To);
            }

            return builder =>
            {
                builder.OpenComponent<ChartTooltip>(0);

                builder.AddAttribute(1, nameof(ChartTooltip.ChildContent), TooltipTemplate?.Invoke((From, To)));

                builder.AddAttribute(2, nameof(ChartTooltip.Title), series?.GetTitle());
                builder.AddAttribute(3, nameof(ChartTooltip.Label), Name);
                builder.AddAttribute(4, nameof(ChartTooltip.Value), $"{from} - {to}");

                if (!string.IsNullOrEmpty(Stroke))
                {
                    builder.AddAttribute(5, nameof(ChartTooltip.Style), $"border: 1px solid {Stroke};");
                }

                builder.AddAttribute(6, nameof(ChartTooltip.Class), "");

                builder.CloseComponent();
            };
        }

        /// <inheritdoc />
        public Point GetTooltipPosition(double mouseX, double mouseY)
        {
            if (Chart == null)
            {
                return new Point { X = mouseX, Y = mouseY };
            }

            if (Chart.ShouldInvertAxes())
            {
                var a = Chart.CategoryScale.Scale(Start);
                var b = Chart.CategoryScale.Scale(End);
                mouseX = Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), mouseX));
                mouseY = Math.Max(0, Math.Min(Chart.ValueScale.OutputSize, mouseY));
            }
            else
            {
                var a = Chart.ValueScale.Scale(Start);
                var b = Chart.ValueScale.Scale(End);
                mouseY = Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), mouseY));
                mouseX = Math.Max(0, Math.Min(Chart.CategoryScale.OutputSize, mouseX));
            }

            return new Point { X = mouseX, Y = mouseY };
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(From), From) ||
                   DidParameterChange(parameters, nameof(To), To) ||
                   base.ShouldRefreshChart(parameters);
        }

        /// <summary>
        /// Removes the reference band from the series overlays.
        /// </summary>
        public void Dispose() => series?.Overlays.Remove(this);
    }
}

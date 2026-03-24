using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSpiderChart component displays multi-dimensional data in a spider web format.
    /// </summary>
    public partial class RadzenSpiderChart : RadzenComponent, IRadzenSpiderChart
    {
        /// <summary>
        /// Gets the legend settings for the spider chart.
        /// </summary>
        internal RadzenSpiderLegend Legend { get; set; } = new RadzenSpiderLegend();

        // Explicit IRadzenSpiderChart implementation to avoid exposing internal members as public API.
        RadzenSpiderLegend IRadzenSpiderChart.Legend
        {
            get => Legend;
            set => Legend = value;
        }

        /// <summary>
        /// Gets or sets child content containing RadzenSpiderSeries components.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the grid shape of the chart.
        /// </summary>
        [Parameter]
        public SpiderChartGridShape GridShape { get; set; } = SpiderChartGridShape.Polygon;

        /// <summary>
        /// Gets or sets the color scheme of the chart.
        /// Available schemes include Pastel (default), Palette, Monochrome, and custom color schemes.
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Gets or sets whether markers are visible.
        /// </summary>
        [Parameter]
        public bool ShowMarkers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether tooltips are shown.
        /// </summary>
        [Parameter]
        public bool ShowTooltip { get; set; } = true;

        /// <summary>
        /// Gets or sets whether numeric axis values are shown along the first radial axis.
        /// </summary>
        [Parameter]
        public bool ShowAxisValues { get; set; }

        /// <summary>
        /// Gets or sets the start angle in degrees (0 = top/north, clockwise). Default is 0.
        /// </summary>
        [Parameter]
        public double StartAngle { get; set; } = 0;

        /// <summary>
        /// Gets or sets the end angle in degrees (0 = top/north, clockwise). Default is 360 (full circle).
        /// </summary>
        [Parameter]
        public double EndAngle { get; set; } = 360;

        /// <summary>
        /// Gets the effective sweep in degrees, handling wrap-around and negative values
        /// (e.g. 270° to 90° = 180°, 180° to -270° = 90°).
        /// </summary>
        private double SweepDegrees
        {
            get
            {
                var sweep = EndAngle - StartAngle;
                // Normalize to (0, 360]
                sweep = sweep % 360;
                if (sweep <= 0) sweep += 360;
                return sweep;
            }
        }

        /// <summary>
        /// Whether the chart displays a partial angular range.
        /// </summary>
        private bool IsPartial => SweepDegrees < 359.99;

        /// <summary>
        /// Event callback for when a series (or a marker) is clicked. Matches <see cref="RadzenChart.SeriesClick" /> behavior.
        /// </summary>
        [Parameter]
        public EventCallback<Radzen.SeriesClickEventArgs> SeriesClick { get; set; }

        [Inject]
        private TooltipService TooltipService { get; set; } = default!;
        private double? Width { get; set; }
        private double? Height { get; set; }
        
        /// <summary>
        /// Gets the series collection.
        /// </summary>
        internal List<IRadzenSpiderSeries> Series { get; } = new();
        
        private IRadzenSpiderSeries? HoveredSeries { get; set; }
        private bool IsLegendHover { get; set; }
        private double MinValue { get; set; }
        private double MaxValue { get; set; } = 100;

        /// <summary>
        /// Gets the visible series.
        /// </summary>
        internal IEnumerable<IRadzenSpiderSeries> VisibleSeries => Series.Where(s => s.IsVisible);

        /// <summary>
        /// Adds a series to the chart.
        /// </summary>
        internal void AddSeries(IRadzenSpiderSeries series)
        {
            if (!Series.Contains(series))
            {
                series.Index = Series.Count;
                Series.Add(series);
                UpdateMinMax();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes a series from the chart.
        /// </summary>
        internal void RemoveSeries(IRadzenSpiderSeries series)
        {
            if (Series.Remove(series))
            {
                UpdateMinMax();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Updates the min and max values based on all series data.
        /// </summary>
        private void UpdateMinMax()
        {
            var allValues = Series.SelectMany(s => s.GetValues()).ToList();
            if (allValues.Count > 0)
            {
                MinValue = Math.Min(0, allValues.Min());
                MaxValue = Math.Max(100, allValues.Max());
            }
        }

        /// <summary>
        /// Gets all unique categories from all series.
        /// </summary>
        private List<string> GetAllCategories()
        {
            var categories = new HashSet<string>();
            foreach (var series in Series)
            {
                foreach (var category in series.GetCategories())
                {
                    if (!string.IsNullOrEmpty(category))
                    {
                        categories.Add(category);
                    }
                }
            }
            return categories.ToList();
        }

        /// <summary>
        /// Gets the index of a category.
        /// </summary>
        private int GetCategoryIndex(string category)
        {
            var categories = GetAllCategories();
            return categories.IndexOf(category);
        }


        /// <summary>
        /// Gets the value for a series at a specific category.
        /// </summary>
        private double GetSeriesValue(IRadzenSpiderSeries series, string category) => series.GetValue(category);

        /// <summary>
        /// Formats a value for display.
        /// </summary>
        private string FormatValue(IRadzenSpiderSeries series, double value) => series.FormatValue(value);

        /// <summary>
        /// Gets the angle in radians for a category at the given index.
        /// </summary>
        private double GetAngle(int index)
        {
            var categories = GetAllCategories();
            if (categories.Count == 0) return -Math.PI / 2;

            var startRad = StartAngle * Math.PI / 180 - Math.PI / 2;
            var sweepRad = SweepDegrees * Math.PI / 180;

            // For partial range, distribute categories at both endpoints
            // For full circle, distribute evenly without doubling the start
            var divisor = IsPartial ? Math.Max(categories.Count - 1, 1) : categories.Count;
            return startRad + index * sweepRad / divisor;
        }

        /// <summary>
        /// Gets the grid path for a specific scale.
        /// </summary>
        private string GetGridPath(double scale)
        {
            var categories = GetAllCategories();
            var points = new List<string>();

            for (int i = 0; i < categories.Count; i++)
            {
                var (x, y) = GetPoint(i, MaxValue * scale);
                points.Add($"{x.ToString("F2", CultureInfo.InvariantCulture)},{y.ToString("F2", CultureInfo.InvariantCulture)}");
            }

            return IsPartial
                ? $"M{string.Join(" L", points)}"
                : $"M{string.Join(" L", points)} Z";
        }

        /// <summary>
        /// Gets an SVG arc path for a circular grid at a specific scale (used for partial ranges).
        /// </summary>
        private string GetArcPath(double scale)
        {
            if (!Width.HasValue || !Height.HasValue) return "";

            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var radius = Math.Min(centerX, centerY) * 0.8 * scale;

            var startRad = StartAngle * Math.PI / 180 - Math.PI / 2;
            var sweepRad = SweepDegrees * Math.PI / 180;
            var endRad = startRad + sweepRad;
            var largeArc = sweepRad > Math.PI ? 1 : 0;

            var x1 = centerX + radius * Math.Cos(startRad);
            var y1 = centerY + radius * Math.Sin(startRad);
            var x2 = centerX + radius * Math.Cos(endRad);
            var y2 = centerY + radius * Math.Sin(endRad);

            return $"M{x1.ToString("F2", CultureInfo.InvariantCulture)},{y1.ToString("F2", CultureInfo.InvariantCulture)} A{radius.ToString("F2", CultureInfo.InvariantCulture)},{radius.ToString("F2", CultureInfo.InvariantCulture)} 0 {largeArc} 1 {x2.ToString("F2", CultureInfo.InvariantCulture)},{y2.ToString("F2", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Gets the series path for rendering.
        /// </summary>
        private string GetSeriesPath(IRadzenSpiderSeries series)
        {
            var categories = GetAllCategories();
            var points = new List<string>();

            foreach (var category in categories)
            {
                var value = GetSeriesValue(series, category);
                var index = GetCategoryIndex(category);
                var (x, y) = GetPoint(index, value);
                points.Add($"{x.ToString("F2", CultureInfo.InvariantCulture)},{y.ToString("F2", CultureInfo.InvariantCulture)}");
            }

            if (points.Count == 0) return "";

            return IsPartial
                ? $"M{string.Join(" L", points)}"
                : $"M{string.Join(" L", points)} Z";
        }

        /// <summary>
        /// Gets the SVG path for a single radial column bar at the given category index.
        /// The bar is a trapezoid/wedge shape centered on the radial axis.
        /// </summary>
        internal string GetColumnPath(int categoryIndex, double value, double halfAngle)
        {
            if (!Width.HasValue || !Height.HasValue) return "";

            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var maxRadius = Math.Min(centerX, centerY) * 0.8;

            var normalizedValue = (value - MinValue) / (MaxValue - MinValue);
            var radius = maxRadius * normalizedValue;

            var angle = GetAngle(categoryIndex);
            var leftAngle = angle - halfAngle;
            var rightAngle = angle + halfAngle;

            // Inner edge at center
            var ix1 = centerX + 0 * Math.Cos(leftAngle);
            var iy1 = centerY + 0 * Math.Sin(leftAngle);
            var ix2 = centerX + 0 * Math.Cos(rightAngle);
            var iy2 = centerY + 0 * Math.Sin(rightAngle);

            // Outer edge at value radius
            var ox1 = centerX + radius * Math.Cos(leftAngle);
            var oy1 = centerY + radius * Math.Sin(leftAngle);
            var ox2 = centerX + radius * Math.Cos(rightAngle);
            var oy2 = centerY + radius * Math.Sin(rightAngle);

            return $"M{centerX.ToInvariantString()},{centerY.ToInvariantString()} L{ox1.ToInvariantString()},{oy1.ToInvariantString()} L{ox2.ToInvariantString()},{oy2.ToInvariantString()} Z";
        }

        /// <summary>
        /// Gets the angular half-width for column bars, accounting for the number of column series.
        /// </summary>
        internal double GetColumnHalfAngle()
        {
            var categories = GetAllCategories();
            if (categories.Count == 0) return 0;

            var sweepRad = SweepDegrees * Math.PI / 180;
            var divisor = IsPartial ? Math.Max(categories.Count - 1, 1) : categories.Count;
            var categoryAngleStep = sweepRad / divisor;

            // Use 40% of the category angle for each column bar
            return categoryAngleStep * 0.4;
        }

        /// <summary>
        /// Calculates the point position for a given index and value.
        /// </summary>
        private (double x, double y) GetPoint(int index, double value)
        {
            if (!Width.HasValue || !Height.HasValue)
            {
                return (0, 0);
            }

            var categories = GetAllCategories();
            if (categories.Count == 0)
            {
                return (Width.Value / 2, Height.Value / 2);
            }

            var angle = GetAngle(index);
            var normalizedValue = (value - MinValue) / (MaxValue - MinValue);
            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var maxRadius = Math.Min(centerX, centerY) * 0.8;
            var radius = maxRadius * normalizedValue;

            return (centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle));
        }

        /// <summary>
        /// Gets the label position for a category.
        /// </summary>
        private (double x, double y) GetLabelPoint(int index)
        {
            if (!Width.HasValue || !Height.HasValue)
            {
                return (0, 0);
            }

            var categories = GetAllCategories();
            if (categories.Count == 0)
            {
                return (Width.Value / 2, Height.Value / 2);
            }

            var angle = GetAngle(index);
            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var maxRadius = Math.Min(centerX, centerY) * 0.8;
            var labelRadius = maxRadius * 1.15;

            return (centerX + labelRadius * Math.Cos(angle), centerY + labelRadius * Math.Sin(angle));
        }

        /// <summary>
        /// Gets the text anchor for a label based on its position.
        /// </summary>
        private string GetLabelAnchor(int index)
        {
            var categories = GetAllCategories();
            if (categories.Count == 0) return "middle";

            var angle = GetAngle(index);
            var x = Math.Cos(angle);

            if (Math.Abs(x) < 0.1)
            {
                return "middle";
            }
            else if (x > 0.1)
            {
                return "start";
            }
            else
            {
                return "end";
            }
        }

        /// <summary>
        /// Gets the baseline for a label based on its position.
        /// </summary>
        private string GetLabelBaseline(int index)
        {
            var categories = GetAllCategories();
            if (categories.Count == 0) return "middle";

            var angle = GetAngle(index);
            var y = Math.Sin(angle);

            if (y < -0.7)
            {
                return "auto";
            }
            else if (y > 0.7)
            {
                return "hanging";
            }
            else
            {
                return "middle";
            }
        }

        /// <summary>
        /// Computes the minimum angular spacing (in degrees) between adjacent categories.
        /// </summary>
        private double AngularSpacingDegrees
        {
            get
            {
                var categories = GetAllCategories();
                if (categories.Count <= 1) return SweepDegrees;
                var divisor = IsPartial ? Math.Max(categories.Count - 1, 1) : categories.Count;
                return SweepDegrees / divisor;
            }
        }

        /// <summary>
        /// Renders the category labels, rotating them along their radial direction when there are many categories.
        /// Skips labels when the angular spacing is too small to prevent overlapping.
        /// </summary>
        private RenderFragment RenderLabels() => builder =>
        {
            var categories = GetAllCategories();
            var rotate = categories.Count > 8;

            // Calculate how many labels to skip to avoid overlap
            var spacing = AngularSpacingDegrees;
            var step = 1;
            if (spacing < 10) step = 4;
            else if (spacing < 15) step = 3;
            else if (spacing < 20) step = 2;

            for (int i = 0; i < categories.Count; i++)
            {
                // Always show first and last labels for partial charts; skip intermediate ones based on step
                var isEndpoint = IsPartial && (i == 0 || i == categories.Count - 1);
                if (!isEndpoint && step > 1 && i % step != 0) continue;

                var (x, y) = GetLabelPoint(i);
                var category = categories[i];
                var xStr = x.ToString("F2", CultureInfo.InvariantCulture);
                var yStr = y.ToString("F2", CultureInfo.InvariantCulture);

                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", xStr);
                builder.AddAttribute(2, "y", yStr);

                if (rotate)
                {
                    var angle = GetAngle(i);
                    var angleDeg = angle * 180 / Math.PI + 90; // Convert so 0° = radial outward
                    var cos = Math.Cos(angle);

                    // Flip labels on the left side so text always reads left-to-right
                    if (cos < -0.01)
                    {
                        angleDeg += 180;
                        builder.AddAttribute(3, "text-anchor", "end");
                    }
                    else
                    {
                        builder.AddAttribute(3, "text-anchor", "start");
                    }

                    builder.AddAttribute(4, "dominant-baseline", "middle");
                    builder.AddAttribute(7, "transform", $"rotate({angleDeg.ToString("F1", CultureInfo.InvariantCulture)},{xStr},{yStr})");
                }
                else
                {
                    builder.AddAttribute(3, "text-anchor", GetLabelAnchor(i));
                    builder.AddAttribute(4, "dominant-baseline", GetLabelBaseline(i));
                }

                builder.AddAttribute(5, "fill", "var(--rz-text-color)");
                builder.AddAttribute(6, "class", "rz-spider-chart-label");
                builder.AddContent(8, category);
                builder.CloseElement();
            }
        };

        /// <summary>
        /// Renders the axis value labels. For partial charts, uses the last radial axis;
        /// for full charts, uses the first radial axis. Labels are offset to the outside of the sweep area.
        /// </summary>
        private RenderFragment RenderAxisValues() => builder =>
        {
            if (!Width.HasValue || !Height.HasValue) return;

            var categories = GetAllCategories();
            if (categories.Count == 0) return;

            // For partial charts, render along the last axis; for full charts, along the first
            var axisIndex = IsPartial ? categories.Count - 1 : 0;
            var axisAngle = GetAngle(axisIndex);
            var midAngle = GetAngle(categories.Count / 2);
            var perp1 = axisAngle + Math.PI / 2;
            var perp2 = axisAngle - Math.PI / 2;

            // Pick the perpendicular that points away from the mid-sweep direction
            var diff1 = Math.Abs(AngleDiff(perp1, midAngle));
            var diff2 = Math.Abs(AngleDiff(perp2, midAngle));
            var perpAngle = diff1 > diff2 ? perp1 : perp2;

            var offsetX = Math.Cos(perpAngle) * 12;
            var offsetY = Math.Sin(perpAngle) * 12;

            // Determine text-anchor based on offset direction
            var cosPerp = Math.Cos(perpAngle);
            string anchor;
            if (Math.Abs(cosPerp) < 0.3) anchor = "middle";
            else if (cosPerp > 0) anchor = "start";
            else anchor = "end";

            for (int i = 1; i <= 5; i++)
            {
                var value = MinValue + (MaxValue - MinValue) * (i / 5.0);
                var (x, y) = GetPoint(axisIndex, value);
                var xStr = (x + offsetX).ToString("F2", CultureInfo.InvariantCulture);
                var yStr = (y + offsetY).ToString("F2", CultureInfo.InvariantCulture);

                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", xStr);
                builder.AddAttribute(2, "y", yStr);
                builder.AddAttribute(3, "text-anchor", anchor);
                builder.AddAttribute(4, "dominant-baseline", "middle");
                builder.AddAttribute(5, "fill", "var(--rz-text-tertiary-color)");
                builder.AddAttribute(6, "class", "rz-spider-chart-axis-value");
                builder.AddAttribute(7, "font-size", "11");
                builder.AddContent(8, value.ToString("F0", CultureInfo.InvariantCulture));
                builder.CloseElement();
            }
        };

        /// <summary>
        /// Returns the signed angular difference normalized to [-PI, PI].
        /// </summary>
        private static double AngleDiff(double a, double b)
        {
            var d = a - b;
            while (d > Math.PI) d -= 2 * Math.PI;
            while (d < -Math.PI) d += 2 * Math.PI;
            return d;
        }

        /// <summary>
        /// Handles series click events.
        /// </summary>
        private async Task InvokeSeriesClick(IRadzenSpiderSeries series, string? category = null, double? value = null, object? data = null)
        {
            if (!SeriesClick.HasDelegate)
            {
                return;
            }

            await SeriesClick.InvokeAsync(new Radzen.SeriesClickEventArgs
            {
                Title = series.Title,
                Category = category,
                Value = value,
                Data = data
            });
        }

        /// <summary>
        /// Handles area mouse enter events.
        /// </summary>
        private void OnAreaMouseEnter(MouseEventArgs args, IRadzenSpiderSeries series)
        {
            HoveredSeries = series;
            StateHasChanged();
        }

        /// <summary>
        /// Handles area mouse leave events.
        /// </summary>
        private void OnAreaMouseLeave()
        {
            if (!IsLegendHover)
            {
                HoveredSeries = null;
                StateHasChanged();
            }
        }

        
        private IRadzenSpiderSeries? currentTooltipSeries;
        private string? currentTooltipCategory;
        
        /// <summary>
        /// Handles marker mouse enter events.
        /// </summary>
        private async Task OnMarkerMouseEnter(MouseEventArgs args, IRadzenSpiderSeries series, string category, double value)
        {
            HoveredSeries = series;
            await ShowMarkerTooltip(args, series, category, value);
        }
        
        
        /// <summary>
        /// Handles marker mouse leave events.
        /// </summary>
        private void OnMarkerMouseLeave()
        {
            HoveredSeries = null;
            HideTooltip();
        }
        
        /// <summary>
        /// Shows tooltip for a marker.
        /// </summary>
        private async Task ShowMarkerTooltip(MouseEventArgs args, IRadzenSpiderSeries series, string category, double value)
        {
            if (!this.ShowTooltip || TooltipService == null)
                return;
            
            // Prevent duplicate tooltips
            if (currentTooltipSeries == series && currentTooltipCategory == category) 
                return;
            
            currentTooltipSeries = series;
            currentTooltipCategory = category;
            
            var valueStr = FormatValue(series, value);

            // Open tooltip using TooltipService
            // IMPORTANT: OffsetX/OffsetY are relative to the event target (SVG element) and are unreliable for positioning
            // inside the chart container. Use ClientX/ClientY translated to chart-local coordinates.
            double x = args.OffsetX;
            double y = args.OffsetY;

            if (JSRuntime != null)
            {
                var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.clientRect", Element);
                x = args.ClientX - rect.Left;
                y = args.ClientY - rect.Top;
            }

            TooltipService.OpenChartTooltip(Element, x, y, tooltipService => builder =>
            {
                builder.OpenComponent<Rendering.ChartTooltip>(0);
                builder.AddAttribute(1, "Title", series.Title ?? "Series");
                builder.AddAttribute(2, "Label", category ?? "");
                builder.AddAttribute(3, "Value", valueStr);
                builder.CloseComponent();
            }, new ChartTooltipOptions());
        }
        
        /// <summary>
        /// Hides the tooltip.
        /// </summary>
        private void HideTooltip()
        {
            currentTooltipSeries = null;
            currentTooltipCategory = null;
            TooltipService?.Close();
        }

        
        /// <summary>
        /// Handles chart mouse leave events.
        /// </summary>
        private void OnChartMouseLeave()
        {
            HoveredSeries = null;
            IsLegendHover = false;
            StateHasChanged();
        }


        /// <summary>
        /// Refreshes the chart.
        /// </summary>
        internal async Task Refresh()
        {
            // Force all series to update to ensure legend items re-render
            foreach (var series in Series)
            {
                series.ForceUpdate();
            }

            await InvokeAsync(StateHasChanged);
        }

        Task IRadzenSpiderChart.Refresh() => Refresh();


        /// <summary>
        /// Highlights a series on hover.
        /// </summary>
        internal void HighlightSeries(IRadzenSpiderSeries series)
        {
            HoveredSeries = series;
            IsLegendHover = true;
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Clears series highlight.
        /// </summary>
        internal void ClearHighlight()
        {
            HoveredSeries = null;
            IsLegendHover = false;
            InvokeAsync(StateHasChanged);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            
            if (firstRender || !Width.HasValue || !Height.HasValue)
            {
                Rendering.Rect rect;

                if (JSRuntime != null)
                {
                    rect = await JSRuntime.InvokeAsync<Rendering.Rect>("Radzen.createResizable", Element, Reference);
                }
                else
                {
                    // Fallback if JSRuntime is not available.
                    rect = new Rendering.Rect { Width = 0, Height = 0 };
                }
                
                if (!Width.HasValue && rect.Width > 0)
                {
                    Width = rect.Width;
                }
                else if (!Width.HasValue)
                {
                    // Fallback if JavaScript sizing fails
                    Width = 800;
                }

                if (!Height.HasValue && rect.Height > 0)
                {
                    Height = rect.Height;
                }
                else if (!Height.HasValue)
                {
                    // Fallback if JavaScript sizing fails
                    Height = 500;
                }
                
                // Legend width handled by flexbox layout
                
                StateHasChanged();
            }
        }

        /// <summary>
        /// Called by JavaScript when the chart is resized.
        /// </summary>
        [JSInvokable]
        public void Resize(double width, double height)
        {
            bool stateHasChanged = false;
            
            if (Width != width)
            {
                Width = width;
                stateHasChanged = true;
            }
            
            if (Height != height)
            {
                Height = height;
                stateHasChanged = true;
            }
            
            // Legend width handled by flexbox layout
            
            if (stateHasChanged)
            {
                StateHasChanged();
            }
        }
        
        /// <inheritdoc />
        public override void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
                JSRuntime!.InvokeVoidAsync("Radzen.disposeElement", Element);
            }

            base.Dispose();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-spider-chart rz-scheme-{ColorScheme.ToString().ToLowerInvariant()}";
        }
    }

    /// <summary>
    /// Spider chart grid shapes.
    /// </summary>
    public enum SpiderChartGridShape
    {
        /// <summary>
        /// Polygon grid shape.
        /// </summary>
        Polygon,
        
        /// <summary>
        /// Circular grid shape.
        /// </summary>
        Circular
    }
}
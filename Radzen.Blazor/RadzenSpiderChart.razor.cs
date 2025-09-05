using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
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
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public partial class RadzenSpiderChart<TItem> : RadzenComponent, IRadzenSpiderChart
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
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Palette;

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
        /// Event callback for when a series is clicked.
        /// </summary>
        [Parameter]
        public EventCallback<RadzenSpiderSeries<TItem>> SeriesClick { get; set; }

        [Inject]
        private TooltipService TooltipService { get; set; } = default!;
        private double? Width { get; set; }
        private double? Height { get; set; }
        
        /// <summary>
        /// Gets the series collection.
        /// </summary>
        internal List<RadzenSpiderSeries<TItem>> Series { get; } = new();
        
        private RadzenSpiderSeries<TItem>? HoveredSeries { get; set; }
        private bool IsLegendHover { get; set; }
        private double MinValue { get; set; }
        private double MaxValue { get; set; } = 100;

        /// <summary>
        /// Gets the visible series.
        /// </summary>
        internal IEnumerable<RadzenSpiderSeries<TItem>> VisibleSeries => Series.Where(s => s.IsVisible);

        /// <summary>
        /// Adds a series to the chart.
        /// </summary>
        internal void AddSeries(RadzenSpiderSeries<TItem> series)
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
        internal void RemoveSeries(RadzenSpiderSeries<TItem> series)
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
            var allValues = new List<double>();
            
            foreach (var series in Series)
            {
                if (series.Data != null && !string.IsNullOrEmpty(series.ValueProperty))
                {
                    var valueGetter = PropertyAccess.Getter<TItem, double>(series.ValueProperty);
                    foreach (var item in series.Data)
                    {
                        allValues.Add(valueGetter(item));
                    }
                }
            }
            
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
                if (series.Data != null && !string.IsNullOrEmpty(series.CategoryProperty))
                {
                    var categoryGetter = PropertyAccess.Getter<TItem, string>(series.CategoryProperty);
                    foreach (var item in series.Data)
                    {
                        var category = categoryGetter(item);
                        if (!string.IsNullOrEmpty(category))
                        {
                            categories.Add(category);
                        }
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
        private double GetSeriesValue(RadzenSpiderSeries<TItem> series, string category)
        {
            if (series.Data == null || string.IsNullOrEmpty(series.CategoryProperty) || string.IsNullOrEmpty(series.ValueProperty))
            {
                return 0;
            }

            var categoryGetter = PropertyAccess.Getter<TItem, string>(series.CategoryProperty);
            var valueGetter = PropertyAccess.Getter<TItem, double>(series.ValueProperty);

            var item = series.Data.FirstOrDefault(d => categoryGetter(d) == category);
            return item != null ? valueGetter(item) : 0;
        }

        /// <summary>
        /// Formats a value for display.
        /// </summary>
        private string FormatValue(RadzenSpiderSeries<TItem> series, double value)
        {
            if (series.ValueFormatter != null)
            {
                return series.ValueFormatter(value);
            }

            if (!string.IsNullOrEmpty(series.FormatString))
            {
                return value.ToString(series.FormatString, CultureInfo.InvariantCulture);
            }

            return value.ToString("F1", CultureInfo.InvariantCulture);
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
                points.Add($"{x:F2},{y:F2}");
            }
            
            return $"M{string.Join(" L", points)} Z";
        }

        /// <summary>
        /// Gets the series path for rendering.
        /// </summary>
        private string GetSeriesPath(RadzenSpiderSeries<TItem> series)
        {
            var categories = GetAllCategories();
            var points = new List<string>();
            
            foreach (var category in categories)
            {
                var value = GetSeriesValue(series, category);
                var index = GetCategoryIndex(category);
                var (x, y) = GetPoint(index, value);
                points.Add($"{x:F2},{y:F2}");
            }
            
            return points.Count > 0 ? $"M{string.Join(" L", points)} Z" : "";
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

            var angleStep = 2 * Math.PI / categories.Count;
            var angle = index * angleStep - Math.PI / 2;
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

            var angleStep = 2 * Math.PI / categories.Count;
            var angle = index * angleStep - Math.PI / 2;
            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var maxRadius = Math.Min(centerX, centerY) * 0.8;
            var labelRadius = maxRadius * 1.15; // Back to original distance
            
            return (centerX + labelRadius * Math.Cos(angle), centerY + labelRadius * Math.Sin(angle));
        }

        /// <summary>
        /// Gets the text anchor for a label based on its position.
        /// </summary>
        private string GetLabelAnchor(int index)
        {
            var categories = GetAllCategories();
            if (categories.Count == 0) return "middle";
            
            var angleStep = 2 * Math.PI / categories.Count;
            var angle = index * angleStep - Math.PI / 2;
            
            // More precise anchoring based on position
            var x = Math.Cos(angle);
            var y = Math.Sin(angle);
            
            // Top and bottom positions
            if (Math.Abs(x) < 0.1)
            {
                return "middle";
            }
            // Right side
            else if (x > 0.1)
            {
                return "start";
            }
            // Left side
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
            
            var angleStep = 2 * Math.PI / categories.Count;
            var angle = index * angleStep - Math.PI / 2;
            
            var y = Math.Sin(angle);
            
            // Top position
            if (y < -0.7)
            {
                return "auto";
            }
            // Bottom position
            else if (y > 0.7)
            {
                return "hanging";
            }
            // Sides
            else
            {
                return "middle";
            }
        }

        /// <summary>
        /// Renders the category labels.
        /// </summary>
        private RenderFragment RenderLabels() => builder =>
        {
            var categories = GetAllCategories();
            
            for (int i = 0; i < categories.Count; i++)
            {
                var (x, y) = GetLabelPoint(i);
                var category = categories[i];
                var anchor = GetLabelAnchor(i);
                var baseline = GetLabelBaseline(i);
                var xStr = x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var yStr = y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                
                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", xStr);
                builder.AddAttribute(2, "y", yStr);
                builder.AddAttribute(3, "text-anchor", anchor);
                builder.AddAttribute(4, "dominant-baseline", baseline);
                builder.AddAttribute(5, "fill", "var(--rz-text-color)");
                builder.AddAttribute(6, "class", "rz-spider-chart-label");
                builder.AddContent(7, category);
                builder.CloseElement();
            }
        };

        /// <summary>
        /// Handles series click events.
        /// </summary>
        private async Task OnSeriesClick(RadzenSpiderSeries<TItem> series, MouseEventArgs args)
        {
            await SeriesClick.InvokeAsync(series);
        }

        /// <summary>
        /// Handles area mouse enter events.
        /// </summary>
        private void OnAreaMouseEnter(MouseEventArgs args, RadzenSpiderSeries<TItem> series)
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

        
        private RadzenSpiderSeries<TItem>? currentTooltipSeries;
        private string? currentTooltipCategory;
        
        /// <summary>
        /// Handles marker mouse enter events.
        /// </summary>
        private void OnMarkerMouseEnter(MouseEventArgs args, RadzenSpiderSeries<TItem> series, string category, double value)
        {
            HoveredSeries = series;
            ShowMarkerTooltip(args, series, category, value);
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
        private void ShowMarkerTooltip(MouseEventArgs args, RadzenSpiderSeries<TItem> series, string category, double value)
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
            TooltipService.OpenChartTooltip(Element, args.OffsetX + 15, args.OffsetY - 5, tooltipService => builder =>
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
        internal void HighlightSeries(RadzenSpiderSeries<TItem> series)
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
        protected override string GetComponentCssClass()
        {
            return "rz-spider-chart";
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
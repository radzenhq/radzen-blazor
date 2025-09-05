using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSpiderChart component.
    /// </summary>
    /// <typeparam name="TItem">The type of data item.</typeparam>
    public partial class RadzenSpiderChart<TItem> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the tooltip service.
        /// </summary>
        [Inject]
        public TooltipService TooltipService { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the category/axis name.
        /// </summary>
        [Parameter]
        public string CategoryProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the value.
        /// </summary>
        [Parameter]
        public string ValueProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which provides the series name.
        /// </summary>
        [Parameter]
        public string SeriesProperty { get; set; }


        /// <summary>
        /// Gets or sets the grid shape.
        /// </summary>
        [Parameter]
        public SpiderChartGridShape GridShape { get; set; } = SpiderChartGridShape.Polygon;

        /// <summary>
        /// Gets or sets the color scheme.
        /// </summary>
        [Parameter]
        public ColorScheme ColorScheme { get; set; } = ColorScheme.Palette;

        /// <summary>
        /// Gets or sets whether to show the legend.
        /// </summary>
        [Parameter]
        public bool ShowLegend { get; set; } = false;

        /// <summary>
        /// A callback that will be invoked when the user clicks on a series.
        /// </summary>
        [Parameter]
        public EventCallback<SeriesClickEventArgs> SeriesClick { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks on a legend.
        /// </summary>
        [Parameter]
        public EventCallback<LegendClickEventArgs> LegendClick { get; set; }

        /// <summary>
        /// Gets or sets the value formatter.
        /// </summary>
        [Parameter]
        public Func<double, string> ValueFormatter { get; set; }

        /// <summary>
        /// Gets or sets the format string for values.
        /// </summary>
        [Parameter]
        public string FormatString { get; set; } = "F0";

        /// <summary>
        /// Gets or sets the legend title text.
        /// </summary>
        [Parameter]
        public string LegendTitleText { get; set; } = "Series";

        /// <summary>
        /// Gets or sets the legend filter placeholder text.
        /// </summary>
        [Parameter]
        public string LegendFilterPlaceholder { get; set; } = "Filter series...";

        /// <summary>
        /// Gets or sets the select all text.
        /// </summary>
        [Parameter]
        public string LegendSelectAllText { get; set; } = "All";

        /// <summary>
        /// Gets or sets the deselect all text.
        /// </summary>
        [Parameter]
        public string LegendDeselectAllText { get; set; } = "None";

        private double? Width { get; set; }
        private double? Height { get; set; }
        private List<SpiderChartSeries> Series { get; set; } = new();
        private List<string> Categories { get; set; } = new();
        private double MaxValue { get; set; } = 100;
        private double MinValue { get; set; } = 0;
        private HashSet<string> HiddenSeries { get; set; } = new();
        private SpiderChartSeries HoveredSeries { get; set; }
        private bool IsLegendHover { get; set; } = false;
        private string legendFilter = "";

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await GetDimensions();
            }
        }

        /// <summary>
        /// Gets the dimensions of the chart element.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task GetDimensions()
        {
            var rect = await JSRuntime.InvokeAsync<Rendering.Rect>("Radzen.createResizable", Element, Reference);
            
            if (!Width.HasValue && rect.Width > 0)
            {
                Width = ShowLegend ? rect.Width - 380 : rect.Width;
            }
            
            if (!Height.HasValue && rect.Height > 0)
            {
                Height = rect.Height;
            }
            
            if (Width.HasValue && Height.HasValue)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Called when the chart is resized.
        /// </summary>
        [JSInvokable]
        public void Resize(double width, double height)
        {
            Width = ShowLegend ? width - 380 : width;
            Height = height;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            ProcessData();
        }

        /// <summary>
        /// Processes the data and creates series and categories.
        /// </summary>
        private void ProcessData()
        {
            if (Data == null || string.IsNullOrEmpty(CategoryProperty) || 
                string.IsNullOrEmpty(ValueProperty)) return;

            Series.Clear();
            Categories.Clear();

            var categoryGetter = PropertyAccess.Getter<TItem, string>(CategoryProperty);
            var valueGetter = PropertyAccess.Getter<TItem, double>(ValueProperty);
            var seriesGetter = string.IsNullOrEmpty(SeriesProperty) ? null : 
                               PropertyAccess.Getter<TItem, string>(SeriesProperty);

            var grouped = seriesGetter != null
                ? Data.GroupBy(item => seriesGetter(item))
                : new[] { Data.GroupBy(_ => "Series").First() };

            var allValues = new List<double>();

            foreach (var group in grouped)
            {
                var seriesData = new Dictionary<string, double>();
                
                foreach (var item in group)
                {
                    var category = categoryGetter(item);
                    var value = valueGetter(item);
                    
                    if (!Categories.Contains(category))
                        Categories.Add(category);
                    
                    seriesData[category] = value;
                    allValues.Add(value);
                }

                Series.Add(new SpiderChartSeries
                {
                    Name = group.Key ?? "Series",
                    Data = seriesData,
                    ColorIndex = Series.Count
                });
            }

            if (allValues.Any())
            {
                MinValue = Math.Min(0, allValues.Min());
                MaxValue = Math.Max(100, allValues.Max());
            }
        }


        /// <summary>
        /// Gets the x,y coordinates for a point on the chart.
        /// </summary>
        /// <param name="axisIndex">The index of the axis/category.</param>
        /// <param name="value">The value at that point.</param>
        /// <returns>A tuple containing the x and y coordinates.</returns>
        private (double x, double y) GetPoint(int axisIndex, double value)
        {
            if (Width == null || Height == null) return (0, 0);

            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var radius = Math.Min(centerX, centerY) * 0.8;

            var angle = (Math.PI * 2 * axisIndex / Categories.Count) - Math.PI / 2;
            var normalizedValue = (value - MinValue) / (MaxValue - MinValue);
            var r = radius * normalizedValue;

            return (centerX + r * Math.Cos(angle), centerY + r * Math.Sin(angle));
        }

        /// <summary>
        /// Gets the x,y coordinates for a category label.
        /// </summary>
        /// <param name="axisIndex">The index of the axis/category.</param>
        /// <returns>A tuple containing the x and y coordinates for the label.</returns>
        private (double x, double y) GetLabelPoint(int axisIndex)
        {
            if (Width == null || Height == null) return (0, 0);

            var centerX = Width.Value / 2;
            var centerY = Height.Value / 2;
            var radius = Math.Min(centerX, centerY) * 0.8;
            
            var angle = (Math.PI * 2 * axisIndex / Categories.Count) - Math.PI / 2;
            var labelDistance = radius * 1.1;
            var x = centerX + labelDistance * Math.Cos(angle);
            var y = centerY + labelDistance * Math.Sin(angle);
            
            return (x, y);
        }

        /// <summary>
        /// Gets the text anchor alignment for a category label.
        /// </summary>
        /// <param name="axisIndex">The index of the axis/category.</param>
        /// <returns>The text-anchor value ("start", "middle", or "end").</returns>
        private string GetLabelAnchor(int axisIndex)
        {
            var angle = (Math.PI * 2 * axisIndex / Categories.Count) - Math.PI / 2;
            var x = Math.Cos(angle);
            var threshold = 0.1;
            
            if (x > threshold)
                return "start";
            else if (x < -threshold)
                return "end";
            else
                return "middle";
        }

        /// <summary>
        /// Gets the dominant baseline for a category label.
        /// </summary>
        /// <param name="axisIndex">The index of the axis/category.</param>
        /// <returns>The dominant-baseline value.</returns>
        private string GetLabelBaseline(int axisIndex)
        {
            var angle = (Math.PI * 2 * axisIndex / Categories.Count) - Math.PI / 2;
            var y = Math.Sin(angle);
            var threshold = 0.1;
            
            if (y < -threshold)
                return "text-after-edge";
            else if (y > threshold)
                return "text-before-edge";
            else
                return "middle";
        }

        /// <summary>
        /// Gets the SVG path string for a series.
        /// </summary>
        /// <param name="series">The series to generate the path for.</param>
        /// <returns>An SVG path string.</returns>
        private string GetSeriesPath(SpiderChartSeries series)
        {
            var points = new List<string>();
            
            for (int i = 0; i < Categories.Count; i++)
            {
                var category = Categories[i];
                var value = series.Data.ContainsKey(category) ? series.Data[category] : 0;
                var (x, y) = GetPoint(i, value);
                
                var xStr = x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var yStr = y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                
                points.Add(i == 0 ? $"M{xStr},{yStr}" : $"L{xStr},{yStr}");
            }

            if (points.Any())
                points.Add("Z");

            return string.Join(" ", points);
        }

        /// <summary>
        /// Gets the SVG path string for a grid level.
        /// </summary>
        /// <param name="level">The level between 0 and 1.</param>
        /// <returns>An SVG path string for the grid.</returns>
        private string GetGridPath(double level)
        {
            var points = new List<string>();
            
            for (int i = 0; i < Categories.Count; i++)
            {
                var (x, y) = GetPoint(i, MinValue + level * (MaxValue - MinValue));
                var xStr = x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                var yStr = y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                points.Add(i == 0 ? $"M{xStr},{yStr}" : $"L{xStr},{yStr}");
            }

            if (points.Any())
                points.Add("Z");

            return string.Join(" ", points);
        }

        /// <summary>
        /// Handles the series click event.
        /// </summary>
        /// <param name="series">The series that was clicked.</param>
        /// <param name="args">The mouse event arguments.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task OnSeriesClick(SpiderChartSeries series, MouseEventArgs args)
        {
            await SeriesClick.InvokeAsync(new SeriesClickEventArgs
            {
                Title = series.Name,
                Data = series.Data.Values.ToArray()
            });
        }

        /// <summary>
        /// Handles the legend item click event.
        /// </summary>
        /// <param name="series">The series whose legend item was clicked.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task OnLegendClick(SpiderChartSeries series)
        {
            if (HiddenSeries.Contains(series.Name))
                HiddenSeries.Remove(series.Name);
            else
                HiddenSeries.Add(series.Name);

            if (LegendClick.HasDelegate)
            {
                await LegendClick.InvokeAsync(new LegendClickEventArgs
                {
                    Title = series.Name,
                    Data = series.Data.Values.ToArray()
                });
            }

            StateHasChanged();
        }

        /// <summary>
        /// Handles the legend checkbox change event.
        /// </summary>
        /// <param name="series">The series whose checkbox was changed.</param>
        /// <param name="isChecked">Whether the checkbox is now checked.</param>
        private void OnLegendCheckboxChange(SpiderChartSeries series, bool isChecked)
        {
            if (isChecked && HiddenSeries.Contains(series.Name))
            {
                HiddenSeries.Remove(series.Name);
            }
            else if (!isChecked && !HiddenSeries.Contains(series.Name))
            {
                HiddenSeries.Add(series.Name);
            }
            
            StateHasChanged();
        }

        /// <summary>
        /// Shows a tooltip for a data point.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <param name="series">The series of the data point.</param>
        /// <param name="category">The category of the data point.</param>
        /// <param name="value">The value of the data point.</param>
        private void ShowTooltip(MouseEventArgs args, SpiderChartSeries series, string category, double value)
        {
            if (TooltipService == null || series == null) return;
            
            if (currentTooltipSeries == series && currentTooltipCategory == category) return;
            currentTooltipSeries = series;
            currentTooltipCategory = category;
            
            var tooltip = new RenderFragment(builder =>
            {
                builder.OpenComponent<Rendering.ChartTooltip>(0);
                builder.AddAttribute(1, "Title", series.Name ?? "Series");
                builder.AddAttribute(2, "Label", category ?? "");
                builder.AddAttribute(3, "Value", FormatValue(value));
                builder.AddAttribute(4, "Class", $"rz-series-{series.ColorIndex}-tooltip");
                builder.CloseComponent();
            });
            
            TooltipService.OpenChartTooltip(Element, args.OffsetX + 15, args.OffsetY - 5, _ => tooltip, new ChartTooltipOptions());
        }

        /// <summary>
        /// Hides the current tooltip.
        /// </summary>
        private void HideTooltip()
        {
            currentTooltipSeries = null;
            currentTooltipCategory = null;
            TooltipService?.Close();
        }
        
        private SpiderChartSeries currentTooltipSeries;
        private string currentTooltipCategory;

        /// <summary>
        /// Handles the mouse enter event for a marker.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <param name="series">The series of the marker.</param>
        /// <param name="category">The category of the marker.</param>
        /// <param name="value">The value of the marker.</param>
        private void OnMarkerMouseEnter(MouseEventArgs args, SpiderChartSeries series, string category, double value)
        {
            if (HoveredSeries != series)
            {
                HoveredSeries = series;
                IsLegendHover = false;
                StateHasChanged();
            }
            ShowTooltip(args, series, category, value);
        }

        /// <summary>
        /// Handles the mouse leave event for a marker.
        /// </summary>
        private void OnMarkerMouseLeave()
        {
            if (HoveredSeries != null && !IsLegendHover)
            {
                HoveredSeries = null;
                StateHasChanged();
            }
            HideTooltip();
        }

        /// <summary>
        /// Handles the mouse enter event for an area.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <param name="series">The series of the area.</param>
        private void OnAreaMouseEnter(MouseEventArgs args, SpiderChartSeries series)
        {
            if (HoveredSeries != series)
            {
                HoveredSeries = series;
                IsLegendHover = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Handles the mouse leave event for an area.
        /// </summary>
        private void OnAreaMouseLeave()
        {
            if (HoveredSeries != null && !IsLegendHover)
            {
                HoveredSeries = null;
                StateHasChanged();
            }
        }


        /// <summary>
        /// Handles the mouse enter event for a legend item.
        /// </summary>
        /// <param name="series">The series of the legend item.</param>
        /// <param name="isHidden">Whether the series is currently hidden.</param>
        private void OnLegendItemMouseEnter(SpiderChartSeries series, bool isHidden)
        {
            if (!isHidden)
            {
                HoveredSeries = series;
                IsLegendHover = true;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Handles the mouse leave event for a legend item.
        /// </summary>
        private void OnLegendItemMouseLeave()
        {
            if (HoveredSeries != null && IsLegendHover)
            {
                HoveredSeries = null;
                IsLegendHover = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Handles the mouse leave event for the entire chart.
        /// </summary>
        private void OnChartMouseLeave()
        {
            if (HoveredSeries != null || IsLegendHover)
            {
                HoveredSeries = null;
                IsLegendHover = false;
                HideTooltip();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Shows a tooltip displaying all values for a series.
        /// </summary>
        /// <param name="args">The mouse event arguments.</param>
        /// <param name="series">The series to show values for.</param>
        private void ShowAreaTooltip(MouseEventArgs args, SpiderChartSeries series)
        {
            if (TooltipService == null || series == null) return;
            
            var tooltip = new RenderFragment(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "rz-chart-tooltip-content");
                builder.AddAttribute(2, "class", $"rz-series-{series.ColorIndex}-tooltip");
                builder.AddAttribute(3, "style", "background: var(--rz-base-background-color); padding: 8px; border-radius: 4px; min-width: 150px; border-width: 2px; border-style: solid;");
                
                builder.OpenElement(4, "div");
                builder.AddAttribute(5, "class", $"rz-series-{series.ColorIndex}");
                builder.AddAttribute(6, "style", "font-weight: bold; margin-bottom: 8px; font-size: 14px; color: inherit;");
                builder.AddContent(7, series.Name ?? "Series");
                builder.CloseElement();
                
                var index = 8;
                foreach (var category in Categories)
                {
                    var value = series.Data.ContainsKey(category) ? series.Data[category] : 0;
                    
                    builder.OpenElement(index++, "div");
                    builder.AddAttribute(index++, "style", "display: flex; justify-content: space-between; gap: 12px; padding: 2px 0; font-size: 12px;");
                    
                    builder.OpenElement(index++, "span");
                    builder.AddAttribute(index++, "style", "color: var(--rz-text-secondary-color);");
                    builder.AddContent(index++, category);
                    builder.CloseElement();
                    
                    builder.OpenElement(index++, "span");
                    builder.AddAttribute(index++, "style", "font-weight: 600;");
                    builder.AddContent(index++, FormatValue(value));
                    builder.CloseElement();
                    
                    builder.CloseElement();
                }
                
                builder.CloseElement();
            });
            
            TooltipService.OpenChartTooltip(Element, args.OffsetX + 15, args.OffsetY - 5, _ => tooltip, new ChartTooltipOptions());
        }

        /// <summary>
        /// Formats a value for display.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>The formatted value string.</returns>
        private string FormatValue(double value)
        {
            if (ValueFormatter != null)
                return ValueFormatter(value);
            
            return value.ToString(FormatString ?? "F0");
        }

        /// <summary>
        /// Gets the filtered series based on the legend filter.
        /// </summary>
        /// <returns>The filtered series collection.</returns>
        private IEnumerable<SpiderChartSeries> GetFilteredSeries()
        {
            if (string.IsNullOrWhiteSpace(legendFilter))
                return Series;
            
            return Series.Where(s => s.Name.Contains(legendFilter, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-spider-chart";
        }

        /// <summary>
        /// Selects all series (makes them visible).
        /// </summary>
        private void SelectAllSeries()
        {
            HiddenSeries.Clear();
            StateHasChanged();
        }

        /// <summary>
        /// Deselects all series (hides them).
        /// </summary>
        private void DeselectAllSeries()
        {
            HiddenSeries.Clear();
            foreach (var series in Series)
            {
                HiddenSeries.Add(series.Name);
            }
            StateHasChanged();
        }

        /// <summary>
        /// Represents a series in the spider chart.
        /// </summary>
        private class SpiderChartSeries
        {
            /// <summary>
            /// Gets or sets the name of the series.
            /// </summary>
            public string Name { get; set; }
            
            /// <summary>
            /// Gets or sets the data points for the series, keyed by category.
            /// </summary>
            public Dictionary<string, double> Data { get; set; }
            
            /// <summary>
            /// Gets or sets the color of the series.
            /// </summary>
            public string Color { get; set; }
            
            /// <summary>
            /// Gets or sets the color index for CSS class generation.
            /// </summary>
            public int ColorIndex { get; set; }
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
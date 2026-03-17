using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A versatile chart component for visualizing data through various chart types including line, area, column, bar, pie, and donut series.
    /// RadzenChart supports multiple series, customizable axes, legends, tooltips, data labels, markers, and interactive features.
    /// Container for one or more chart series components. Each series (RadzenLineSeries, RadzenColumnSeries, RadzenAreaSeries, etc.) defines how data is visualized.
    /// Supports Cartesian charts (Line, Area, Column, Bar, StackedColumn, StackedBar, StackedArea with X/Y axes), Pie charts (Pie and Donut series for showing proportions),
    /// customization of color schemes/axis configuration/grid lines/legends/tooltips/data labels/markers, interactive click events on series and legend items with hover tooltips,
    /// annotations including trend lines/mean/median/mode lines/value annotations, and responsive design that automatically adapts to container size.
    /// Series are defined as child components within the RadzenChart. Configure axes using RadzenCategoryAxis and RadzenValueAxis, customize the legend with RadzenLegend, and add tooltips with RadzenChartTooltipOptions.
    /// </summary>
    /// <example>
    /// Basic column chart:
    /// <code>
    ///   &lt;RadzenChart&gt;
    ///       &lt;RadzenColumnSeries Data=@revenue CategoryProperty="Quarter" Title="Revenue" ValueProperty="Revenue" /&gt;
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
    /// Chart with multiple series and custom legend:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenLineSeries Data=@sales2023 CategoryProperty="Month" ValueProperty="Amount" Title="2023 Sales" /&gt;
    ///     &lt;RadzenLineSeries Data=@sales2024 CategoryProperty="Month" ValueProperty="Amount" Title="2024 Sales" /&gt;
    ///     &lt;RadzenLegend Position="LegendPosition.Bottom" /&gt;
    ///     &lt;RadzenCategoryAxis Formatter="@(value =&gt; value.ToString())" /&gt;
    ///     &lt;RadzenValueAxis Formatter="@(value =&gt; value.ToString("C"))" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenChart : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the color scheme used to assign colors to chart series.
        /// Determines the palette of colors applied sequentially to each series when series-specific colors are not set.
        /// Available schemes include Pastel (default), Palette, Monochrome, and custom color schemes.
        /// </summary>
        /// <value>The color scheme. Default uses the Pastel scheme.</value>
        [Parameter]
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether series highlight on hover is enabled.
        /// When <c>true</c>, hovering over a series or its legend item highlights the series and dims the others.
        /// </summary>
        /// <value><c>true</c> if series hover is allowed; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool AllowSeriesHover { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when a user clicks on a data point or segment in a chart series.
        /// Provides information about the clicked series, data item, and value in the event arguments.
        /// </summary>
        /// <value>The series click event callback.</value>
        [Parameter]
        public EventCallback<SeriesClickEventArgs> SeriesClick { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when a user clicks on a legend item.
        /// Useful for implementing custom behaviors like toggling series visibility or filtering data.
        /// </summary>
        /// <value>The legend click event callback.</value>
        [Parameter]
        public EventCallback<LegendClickEventArgs> LegendClick { get; set; }

        [Inject]
        TooltipService? TooltipService { get; set; }

        /// <summary>
        /// Gets the runtime width of the chart.
        /// </summary>
        protected double? Width { get; set; }

        /// <summary>
        /// Gets the runtime height of the chart.
        /// </summary>
        protected double? Height { get; set; }

        /// <summary>
        /// Gets or sets the top margin of the plot area.
        /// </summary>
        protected double MarginTop { get; set; }

        /// <summary>
        /// Gets or sets the left margin of the plot area.
        /// </summary>
        protected double MarginLeft { get; set; }

        /// <summary>
        /// Gets or sets the right margin of the plot area.
        /// </summary>
        protected double MarginRight { get; set; }

        /// <summary>
        /// Gets or sets the bottom margin of the plot area.
        /// </summary>
        protected double MarginBottom { get; set; }

        /// <summary>
        /// Gets or sets the child content. Used to specify series and other configuration.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        internal ScaleBase CategoryScale { get; set; } = new LinearScale();
        internal ScaleBase ValueScale { get; set; } = new LinearScale();
        internal Dictionary<string, ScaleBase> AdditionalValueScales { get; set; } = new Dictionary<string, ScaleBase>();
        internal IList<IChartSeries> Series { get; set; } = new List<IChartSeries>();
        internal RadzenColumnOptions ColumnOptions { get; set; } = new RadzenColumnOptions();
        internal RadzenBarOptions BarOptions { get; set; } = new RadzenBarOptions();
        internal RadzenLegend Legend { get; set; } = new RadzenLegend();
        internal RadzenCategoryAxis CategoryAxis { get; set; } = new RadzenCategoryAxis();
        internal RadzenValueAxis ValueAxis { get; set; } = new RadzenValueAxis();
        internal Dictionary<string, RadzenValueAxis> AdditionalValueAxes { get; set; } = new Dictionary<string, RadzenValueAxis>();
        internal RadzenChartTooltipOptions Tooltip { get; set; } = new RadzenChartTooltipOptions();

        /// <summary>
        /// Gets or sets whether mouse wheel zoom is enabled.
        /// </summary>
        [Parameter]
        public bool AllowZoom { get; set; }

        /// <summary>
        /// Gets or sets whether pan via scrollbar is enabled.
        /// </summary>
        [Parameter]
        public bool AllowPan { get; set; }

        /// <summary>
        /// Gets or sets the zoom level as a percentage. A value of 100 means no zoom (full range visible).
        /// Higher values zoom in (e.g., 200 shows half the range, 400 shows a quarter).
        /// Set to 100 to reset zoom. Supports two-way binding with <c>@bind-Zoom</c>.
        /// </summary>
        [Parameter]
        public double Zoom { get; set; } = 100;

        /// <summary>
        /// Gets or sets the callback invoked when the zoom level changes due to user interaction (mouse wheel or pan).
        /// Used for two-way binding with <c>@bind-Zoom</c>.
        /// </summary>
        [Parameter]
        public EventCallback<double> ZoomChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the visible range changes due to zoom or pan.
        /// Provides the current zoom level and visible range fractions.
        /// </summary>
        [Parameter]
        public EventCallback<ChartViewChangeEventArgs> ViewChange { get; set; }

        /// <summary>
        /// Gets or sets the start of the visible range as a fraction (0-1) of the full category range.
        /// </summary>
        internal double ZoomStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the visible range as a fraction (0-1) of the full category range.
        /// </summary>
        internal double ZoomEnd { get; set; } = 1;

        /// <summary>
        /// The full category scale input range before zoom is applied.
        /// </summary>
        private double fullCategoryStart;
        private double fullCategoryEnd;

        private void ApplyZoomLevel()
        {
            var zoomLevel = Math.Max(100, Zoom) / 100.0;
            var range = 1.0 / zoomLevel;
            var center = (ZoomStart + ZoomEnd) / 2;
            ZoomStart = Math.Max(0, center - range / 2);
            ZoomEnd = Math.Min(1, ZoomStart + range);
            if (ZoomStart < 0) { ZoomStart = 0; ZoomEnd = range; }
        }

        private async Task NotifyZoomChanged()
        {
            var range = ZoomEnd - ZoomStart;
            var newZoom = range > 0 ? Math.Round(100.0 / range) : 100;
            Zoom = newZoom;
            await ZoomChanged.InvokeAsync(newZoom);
            await ViewChange.InvokeAsync(new ChartViewChangeEventArgs
            {
                Zoom = newZoom,
                ViewStart = ZoomStart,
                ViewEnd = ZoomEnd
            });
        }

        /// <summary>
        /// The bottom offset for the scrollbar, to position it above the legend.
        /// </summary>
        private double scrollbarBottom;

        internal void AddValueAxis(string name, RadzenValueAxis axis)
        {
            AdditionalValueAxes[name] = axis;
        }

        internal void RemoveValueAxis(string name)
        {
            AdditionalValueAxes.Remove(name);
            AdditionalValueScales.Remove(name);
        }

        internal ScaleBase GetValueScale(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return ValueScale;
            }

            return AdditionalValueScales.TryGetValue(name, out var scale) ? scale : ValueScale;
        }

        internal RadzenValueAxis GetValueAxis(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return ValueAxis;
            }

            return AdditionalValueAxes.TryGetValue(name, out var axis) ? axis : ValueAxis;
        }
        internal void AddSeries(IChartSeries series)
        {
            if (!Series.Contains(series))
            {
                Series.Add(series);
            }
        }

        internal void RemoveSeries(IChartSeries series)
        {
            Series.Remove(series);
        }
        /// <summary>
        /// Returns the Series used by the Chart.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IChartSeries> GetSeries() => Series.ToList();

        /// <summary>
        /// Returns whether the chart should render axes.
        /// </summary>
        /// <returns></returns>
        protected bool ShouldRenderAxes()
        {
            var pieType = typeof(RadzenPieSeries<>);
            var donutType = typeof(RadzenDonutSeries<>);
            var funnelType = typeof(RadzenFunnelSeries<>);
            var pyramidType = typeof(RadzenPyramidSeries<>);

            return !Series.All(series =>
            {
                var type = series.GetType().GetGenericTypeDefinition();

                return type == pieType || type == donutType || type == funnelType || type == pyramidType;
            });
        }

        internal bool ShouldInvertAxes()
        {
            return Series.Count > 0 && Series.All(series => series is IChartBarSeries);
        }

        /// <summary>
        /// Updates the scales based on the configuration.
        /// </summary>
        /// <returns></returns>
        protected virtual bool UpdateScales()
        {
            var valueScale = ValueScale;
            var categoryScale = CategoryScale;

            CategoryScale = new LinearScale { Output = CategoryScale.Output };
            ValueScale = ValueAxis.Logarithmic
                ? new LogarithmicScale { Base = ValueAxis.LogarithmicBase, Output = ValueScale.Output }
                : new LinearScale { Output = ValueScale.Output };

            var visibleSeries = Series.Where(series => series.Visible).ToList();
            var invisibleSeries = Series.Where(series => series.Visible == false).ToList();

            if (visibleSeries.Count == 0 && invisibleSeries.Count > 0)
            {
                visibleSeries.Add(invisibleSeries.Last());
            }

            // Partition series: primary axis vs named axes
            var primarySeries = new List<IChartSeries>();
            var namedAxisSeries = new Dictionary<string, List<IChartSeries>>();

            foreach (var series in visibleSeries)
            {
                var axisName = (series is IChartValueAxisSeries named) ? named.ValueAxisName : null;
                if (string.IsNullOrEmpty(axisName))
                {
                    primarySeries.Add(series);
                }
                else
                {
                    if (!namedAxisSeries.TryGetValue(axisName, out var list))
                    {
                        list = new List<IChartSeries>();
                        namedAxisSeries[axisName] = list;
                    }
                    list.Add(series);
                }
            }

            // All series contribute to category scale
            foreach (var series in visibleSeries)
            {
                CategoryScale = series.TransformCategoryScale(CategoryScale);
            }

            // Primary series contribute to primary value scale
            foreach (var series in primarySeries)
            {
                ValueScale = series.TransformValueScale(ValueScale);
            }

            // Named-axis series contribute to their own scales
            foreach (var entry in namedAxisSeries)
            {
                var axisName = entry.Key;
                var axis = GetValueAxis(axisName);

                ScaleBase CreateAdditionalScale(ScaleRange? output = null)
                {
                    if (axis.Logarithmic)
                    {
                        var s = new LogarithmicScale { Base = axis.LogarithmicBase };
                        if (output != null) s.Output = output;
                        return s;
                    }
                    var ls = new LinearScale();
                    if (output != null) ls.Output = output;
                    return ls;
                }

                ScaleBase additionalScale;
                if (!AdditionalValueScales.TryGetValue(axisName, out var existingScale))
                {
                    additionalScale = CreateAdditionalScale();
                }
                else
                {
                    additionalScale = CreateAdditionalScale(existingScale.Output);
                }

                foreach (var series in entry.Value)
                {
                    additionalScale = series.TransformValueScale(additionalScale);
                }

                AdditionalValueScales[axisName] = additionalScale;
            }

            // Remove scales for axes that no longer have series
            foreach (var key in AdditionalValueScales.Keys.ToList())
            {
                if (!namedAxisSeries.ContainsKey(key))
                {
                    AdditionalValueScales.Remove(key);
                }
            }

            AxisBase xAxis = CategoryAxis;
            AxisBase yAxis = ValueAxis;

            if (ShouldInvertAxes())
            {
                xAxis = ValueAxis;
                yAxis = CategoryAxis;
            }
            else
            {
                CategoryScale.Padding = CategoryAxis.Padding;
            }

            CategoryScale.Resize(xAxis.Min!, xAxis.Max!);

            if (xAxis.Step != null)
            {
                CategoryScale.Step = xAxis.Step;
                CategoryScale.Round = false;
            }

            ValueScale.Resize(yAxis.Min!, yAxis.Max!);

            if (yAxis.Step != null)
            {
                ValueScale.Step = yAxis.Step;
                ValueScale.Round = false;
            }

            // Resize additional value scales
            foreach (var entry in AdditionalValueScales)
            {
                var axis = GetValueAxis(entry.Key);
                entry.Value.Resize(axis.Min!, axis.Max!);
                if (axis.Step != null)
                {
                    entry.Value.Step = axis.Step;
                    entry.Value.Round = false;
                }
            }

            var legendSize = Legend.Measure(this);
            var valueAxisSize = ValueAxis.Measure(this);
            var categoryAxisSize = CategoryAxis.Measure(this);

            // Measure additional axes for right margin
            double additionalAxesWidth = 0;
            foreach (var entry in AdditionalValueAxes)
            {
                additionalAxesWidth += entry.Value.Measure(this, GetValueScale(entry.Key));
            }

            if (!ShouldRenderAxes())
            {
                valueAxisSize = categoryAxisSize = 0;
                additionalAxesWidth = 0;
            }

            MarginTop = 32;
            MarginRight = 32 + additionalAxesWidth;
            MarginLeft = valueAxisSize;
            MarginBottom = Math.Max(32, categoryAxisSize);

            if (Legend.Visible)
            {
                if (Legend.Position == LegendPosition.Right || Legend.Position == LegendPosition.Left)
                {
                    if (Legend.Position == LegendPosition.Right)
                    {
                        MarginRight = legendSize + 16 + additionalAxesWidth;
                    }
                    else
                    {
                        MarginLeft = legendSize + 16 + valueAxisSize;
                    }
                }
                else if (Legend.Position == LegendPosition.Top || Legend.Position == LegendPosition.Bottom)
                {
                    if (Legend.Position == LegendPosition.Top)
                    {
                        MarginTop = legendSize + 16;
                    }
                    else
                    {
                        MarginBottom = legendSize + 16 + categoryAxisSize;
                    }
                }
            }

            if (AllowZoom || AllowPan)
            {
                MarginBottom += 20;

                scrollbarBottom = 0;
                if (Legend.Visible && Legend.Position == LegendPosition.Bottom)
                {
                    scrollbarBottom = legendSize + 16;
                }
            }

            var categoryStart = MarginLeft;
            var categoryEnd = Width != null ? Width.Value - MarginRight : 0;
            var valueStart = Height != null ? Height.Value - MarginBottom : 0;
            var valueEnd = MarginTop;

            CategoryScale.Output = new ScaleRange
            {
                Start = CategoryAxis.Inverted ? categoryEnd : categoryStart,
                End = CategoryAxis.Inverted ? categoryStart : categoryEnd
            };
            ValueScale.Output = new ScaleRange
            {
                Start = ValueAxis.Inverted ? valueEnd : valueStart,
                End = ValueAxis.Inverted ? valueStart : valueEnd
            };

            ValueScale.Fit(ValueAxis.TickDistance);
            CategoryScale.Fit(CategoryAxis.TickDistance);

            // Apply zoom to category scale
            if ((AllowZoom || AllowPan) && (ZoomStart > 0 || ZoomEnd < 1))
            {
                fullCategoryStart = CategoryScale.Input.Start;
                fullCategoryEnd = CategoryScale.Input.End;

                var fullRange = fullCategoryEnd - fullCategoryStart;
                CategoryScale.Input = new ScaleRange
                {
                    Start = fullCategoryStart + fullRange * ZoomStart,
                    End = fullCategoryStart + fullRange * ZoomEnd
                };
                CategoryScale.Round = false;

                // Recalculate step for zoomed range
                var zoomedTicks = CategoryScale.Ticks(CategoryAxis.TickDistance);
                CategoryScale.Step = zoomedTicks.Step;
            }
            else
            {
                fullCategoryStart = CategoryScale.Input.Start;
                fullCategoryEnd = CategoryScale.Input.End;
            }

            // Set output ranges and fit additional scales
            foreach (var entry in AdditionalValueScales)
            {
                var axis = GetValueAxis(entry.Key);
                entry.Value.Output = new ScaleRange
                {
                    Start = axis.Inverted ? valueEnd : valueStart,
                    End = axis.Inverted ? valueStart : valueEnd
                };
                entry.Value.Fit(axis.TickDistance);
            }

            var stateHasChanged = !ValueScale.IsEqualTo(valueScale);

            if (!CategoryScale.IsEqualTo(categoryScale))
            {
                stateHasChanged = true;
            }

            return stateHasChanged;
        }

        /// <summary>
        /// Invoked via interop when the RadzenChart resizes. Display the series with the new dimensions.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        [JSInvokable]
        public async Task Resize(double width, double height)
        {
            var stateHasChanged = false;

            if (width != Width)
            {
                Width = width;
                stateHasChanged = true;

                var cs = MarginLeft;
                var ce = Width.Value - MarginRight;
                CategoryScale.Output = new ScaleRange
                {
                    Start = CategoryAxis.Inverted ? ce : cs,
                    End = CategoryAxis.Inverted ? cs : ce
                };
            }

            if (height != Height)
            {
                Height = height;
                stateHasChanged = true;

                var vs = Height.Value - MarginBottom;
                var ve = MarginTop;
                ValueScale.Output = new ScaleRange
                {
                    Start = ValueAxis.Inverted ? ve : vs,
                    End = ValueAxis.Inverted ? vs : ve
                };

                foreach (var entry in AdditionalValueScales)
                {
                    var axis = GetValueAxis(entry.Key);
                    entry.Value.Output = new ScaleRange
                    {
                        Start = axis.Inverted ? ve : vs,
                        End = axis.Inverted ? vs : ve
                    };
                }
            }

            if (stateHasChanged)
            {
                await Refresh();
            }
        }

        RenderFragment? tooltip;
        object? tooltipData;
        double mouseX;
        double mouseY;

        /// <summary>
        /// Invoked via interop when the user moves the mouse over the RadzenChart. Displays the tooltip.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [JSInvokable]
        public async Task MouseMove(double x, double y)
        {
            mouseX = x;
            mouseY = y;

            await DisplayTooltip();
        }

        /// <summary>
        /// The minimum pixel distance from a data point to the mouse cursor required for the SeriesClick event to fire. Set to 25 by default.
        /// </summary>
        [Parameter]
        public int ClickTolerance { get; set; } = 25;

        /// <summary>
        /// The minimum pixel distance from a data point to the mouse cursor required by the tooltip to show. Set to 25 by default.
        /// </summary>
        [Parameter]
        public int TooltipTolerance { get; set; } = 25;

        /// <summary>
        /// Invoked via interop when the user clicks the RadzenChart. Raises the <see cref="SeriesClick" /> handler.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [JSInvokable]
        public async Task Click(double x, double y)
        {
            IChartSeries? closestSeries = null;
            object? closestSeriesData = null;
            double closestSeriesDistanceSquared = ClickTolerance * ClickTolerance;

            var queryX = x - MarginLeft;
            var queryY = y - MarginTop;

            foreach (var series in Series)
            {
                if (series.Visible)
                {
                    var (seriesData, seriesDataPoint) = series.DataAt(queryX, queryY);
                    if (seriesData != null)
                    {
                        double xDelta = queryX - seriesDataPoint.X;
                        double yDelta = queryY - seriesDataPoint.Y;
                        double squaredDistance = xDelta * xDelta + yDelta * yDelta;
                        if (squaredDistance < closestSeriesDistanceSquared)
                        {
                            closestSeries = series;
                            closestSeriesData = seriesData;
                            closestSeriesDistanceSquared = squaredDistance;
                        }
                    }
                }
            }

            if (closestSeriesData != null && closestSeries != null)
            {
                await closestSeries.InvokeClick(SeriesClick, closestSeriesData);
            }
        }

        /// <summary>
        /// Invoked via interop when the user scrolls the mouse wheel over the chart. Zooms in or out.
        /// </summary>
        /// <param name="x">The mouse X position relative to the chart element.</param>
        /// <param name="delta">Positive to zoom out, negative to zoom in.</param>
        [JSInvokable]
        public async Task OnWheel(double x, int delta)
        {
            if (!AllowZoom) return;

            var plotWidth = (Width ?? 0) - MarginLeft - MarginRight;
            if (plotWidth <= 0) return;

            var fraction = Math.Clamp((x - MarginLeft) / plotWidth, 0, 1);
            var zoomFactor = delta < 0 ? 0.8 : 1.25;
            var range = ZoomEnd - ZoomStart;
            var newRange = Math.Clamp(range * zoomFactor, 0.01, 1.0);

            var center = ZoomStart + fraction * range;
            var newStart = center - newRange * fraction;
            var newEnd = newStart + newRange;

            if (newEnd > 1) { newEnd = 1; newStart = 1 - newRange; }
            if (newStart < 0) { newStart = 0; newEnd = newRange; }

            ZoomStart = Math.Max(0, newStart);
            ZoomEnd = Math.Min(1, newEnd);

            await NotifyZoomChanged();
            await Refresh();
        }

        /// <summary>
        /// Invoked via interop when the user drags the scrollbar thumb.
        /// </summary>
        /// <param name="position">The new start position as a fraction (0-1).</param>
        [JSInvokable]
        public async Task OnPan(double position)
        {
            if (!AllowPan && !AllowZoom) return;

            var range = ZoomEnd - ZoomStart;
            ZoomStart = Math.Clamp(position, 0, 1 - range);
            ZoomEnd = ZoomStart + range;

            await NotifyZoomChanged();
            await Refresh();
        }

        internal async Task DisplayTooltip()
        {
            if (Tooltip.Visible)
            {
                var orderedSeries = Series.OrderBy(s => s.RenderingOrder).Reverse();
                IChartSeries? closestSeries = null;
                object? closestSeriesData = null;
                double closestSeriesDistanceSquared = TooltipTolerance * TooltipTolerance;

                var queryX = mouseX - MarginLeft;
                var queryY = mouseY - MarginTop;

                foreach (var series in orderedSeries)
                {
                    if (series.Visible)
                    {
                        foreach (var overlay in series.Overlays.Reverse())
                        {
                            if (overlay.Visible && overlay.Contains(queryX, queryY, TooltipTolerance))
                            {
                                tooltipData = null;
                                tooltip = overlay.RenderTooltip(queryX, queryY);
                                var tooltipPosition = overlay.GetTooltipPosition(queryX, queryY);
                                TooltipService?.OpenChartTooltip(Element, tooltipPosition.X + MarginLeft, tooltipPosition.Y + MarginTop, _ => tooltip, new ChartTooltipOptions
                                {
                                    ColorScheme = ColorScheme
                                });
                                await Task.Yield();

                                return;
                            }
                        }

                        var (seriesData, seriesDataPoint) = series.DataAt(queryX, queryY);
                        if (seriesData != null)
                        {
                            double xDelta = queryX - seriesDataPoint.X;
                            double yDelta = queryY - seriesDataPoint.Y;
                            double squaredDistance = xDelta * xDelta + yDelta * yDelta;
                            if (squaredDistance < closestSeriesDistanceSquared)
                            {
                                closestSeries = series;
                                closestSeriesData = seriesData;
                                closestSeriesDistanceSquared = squaredDistance;
                            }
                        }
                    }
                }

                if (closestSeriesData != null && closestSeries != null)
                {
                    if (closestSeriesData != tooltipData)
                    {
                        tooltipData = closestSeriesData;
                        tooltip = closestSeries.RenderTooltip(closestSeriesData);
                        var tooltipPosition = closestSeries.GetTooltipPosition(closestSeriesData);
                        TooltipService?.OpenChartTooltip(Element, tooltipPosition.X + MarginLeft, tooltipPosition.Y + MarginTop, _ => tooltip, new ChartTooltipOptions
                        {
                            ColorScheme = ColorScheme
                        });
                        await Task.Yield();
                    }
                    return;
                }
            }

            if (tooltip != null)
            {
                tooltipData = null;
                tooltip = null;

                TooltipService?.Close();
                await Task.Yield();
            }
        }

        /// <summary>
        /// Displays a Tooltip on a chart without user interaction, given a series, and the data associated with it.
        /// </summary>
        /// <param name="series"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        public async Task DisplayTooltipFor(IChartSeries series, object data)
        {
            ArgumentNullException.ThrowIfNull(series);
            if (!Series.Contains(series))
            {
                throw new ArgumentException($"Series:{series.GetTitle()} does not exist in {nameof(this.Series)}");
            }

            if (IsJSRuntimeAvailable)
            {
                var point = series.GetTooltipPosition(data);
                await MouseMove(point.X + MarginLeft, point.Y + MarginTop);
            }
        }

        internal async Task ShowTooltip(IChartSeries series, object data)
        {
            if (IsJSRuntimeAvailable)
            {
                tooltipData = data;
                tooltip = series.RenderTooltip(data);
                var point = series.GetTooltipPosition(data);
                TooltipService?.OpenChartTooltip(Element, point.X + MarginLeft, point.Y + MarginTop, _ => tooltip, new ChartTooltipOptions
                {
                    ColorScheme = ColorScheme
                });
                await Task.Yield();
            }
        }

        private bool widthAndHeightAreSet;
        private bool firstRender = true;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible && JSRuntime != null)
                {
                    var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.createChart", Element, Reference);

                    if (!widthAndHeightAreSet)
                    {
                        widthAndHeightAreSet = true;

                        await Resize(rect.Width, rect.Height);
                    }
                }
            }

        }

        internal string? ClipPath { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ClipPath = $"clipPath{UniqueID}";
            CategoryAxis.Chart = this;
            ValueAxis.Chart = this;

            ApplyZoomLevel();

            Initialize();
        }

        private void Initialize()
        {
            double width = 0;
            double height = 0;

            if (CurrentStyle.TryGetValue("height", out var pixelHeight))
            {
                if (pixelHeight.EndsWith("px", StringComparison.Ordinal))
                {
                    height = Convert.ToDouble(pixelHeight.TrimEnd("px".ToCharArray()), CultureInfo.InvariantCulture);
                }
            }

            if (CurrentStyle.TryGetValue("width", out var pixelWidth))
            {

                if (pixelWidth.EndsWith("px", StringComparison.Ordinal))
                {
                    width = Convert.ToDouble(pixelWidth.TrimEnd("px".ToCharArray()), CultureInfo.InvariantCulture);
                }
            }

            if (width > 0 && height > 0)
            {
                widthAndHeightAreSet = true;

                Width = width;
                Height = height;

                CategoryScale.Output = new ScaleRange { Start = MarginLeft, End = Width.Value - MarginRight };
                ValueScale.Output = new ScaleRange { Start = Height.Value - MarginBottom, End = MarginTop };
            }
        }

        private bool visibleChanged;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldRefresh = parameters.DidParameterChange(nameof(Style), Style);
            bool zoomChanged = parameters.DidParameterChange(nameof(Zoom), Zoom);

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (zoomChanged)
            {
                ApplyZoomLevel();
            }

            if (shouldRefresh)
            {
                Initialize();
            }

            if (visibleChanged && !firstRender)
            {
                if (Visible == false && JSRuntime != null)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.destroyChart", Element);
                }
            }
        }

        internal async Task Refresh(bool force = true)
        {
            if (widthAndHeightAreSet)
            {

                var stateHasChanged = UpdateScales();

                if (stateHasChanged || force)
                {
                    StateHasChanged();
                    await DisplayTooltip();
                }
            }
        }

        /// <summary>
        /// Causes all series to refresh. Use it when <see cref="CartesianSeries{TItem}.Data" /> has changed.
        /// </summary>
        public async Task Reload()
        {
            await Refresh(true);
        }

        /// <summary>
        /// Resets zoom and pan to show the full data range.
        /// </summary>
        public async Task ResetZoom()
        {
            ZoomStart = 0;
            ZoomEnd = 1;
            await NotifyZoomChanged();
            await Refresh(true);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable && JSRuntime != null)
            {
                JSRuntime.InvokeVoid("Radzen.destroyChart", Element);
            }

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var css = $"rz-chart rz-scheme-{ColorScheme.ToString().ToLowerInvariant()}";

            if (AllowSeriesHover)
            {
                css += " rz-chart-series-hover";
            }

            return css;
        }
    }
}

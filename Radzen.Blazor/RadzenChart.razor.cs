using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
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
        /// Available schemes include Pastel, Palette (default), Monochrome, and custom color schemes.
        /// </summary>
        /// <value>The color scheme. Default uses the Palette scheme.</value>
        [Parameter]
        public ColorScheme ColorScheme { get; set; }

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
        TooltipService TooltipService { get; set; }

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
        public RenderFragment ChildContent { get; set; }

        internal ScaleBase CategoryScale { get; set; } = new LinearScale();
        internal ScaleBase ValueScale { get; set; } = new LinearScale();
        internal IList<IChartSeries> Series { get; set; } = new List<IChartSeries>();
        internal RadzenColumnOptions ColumnOptions { get; set; } = new RadzenColumnOptions();
        internal RadzenBarOptions BarOptions { get; set; } = new RadzenBarOptions();
        internal RadzenLegend Legend { get; set; } = new RadzenLegend();
        internal RadzenCategoryAxis CategoryAxis { get; set; } = new RadzenCategoryAxis();
        internal RadzenValueAxis ValueAxis { get; set; } = new RadzenValueAxis();
        internal RadzenChartTooltipOptions Tooltip { get; set; } = new RadzenChartTooltipOptions();
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

            return !Series.All(series =>
            {
                var type = series.GetType().GetGenericTypeDefinition();

                return type == pieType || type == donutType;
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
            ValueScale = new LinearScale { Output = ValueScale.Output };

            var visibleSeries = Series.Where(series => series.Visible).ToList();
            var invisibleSeries = Series.Where(series => series.Visible == false).ToList();

            if (!visibleSeries.Any() && invisibleSeries.Any())
            {
                visibleSeries.Add(invisibleSeries.Last());
            }

            foreach (var series in visibleSeries)
            {
                CategoryScale = series.TransformCategoryScale(CategoryScale);
                ValueScale = series.TransformValueScale(ValueScale);
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

            CategoryScale.Resize(xAxis.Min, xAxis.Max);

            if (xAxis.Step != null)
            {
                CategoryScale.Step = xAxis.Step;
                CategoryScale.Round = false;
            }

            ValueScale.Resize(yAxis.Min, yAxis.Max);

            if (yAxis.Step != null)
            {
                ValueScale.Step = yAxis.Step;
                ValueScale.Round = false;
            }

            var legendSize = Legend.Measure(this);
            var valueAxisSize = ValueAxis.Measure(this);
            var categoryAxisSize = CategoryAxis.Measure(this);

            if (!ShouldRenderAxes())
            {
                valueAxisSize = categoryAxisSize = 0;
            }

            MarginTop = MarginRight = 32;
            MarginLeft = valueAxisSize;
            MarginBottom = Math.Max(32, categoryAxisSize);

            if (Legend.Visible)
            {
                if (Legend.Position == LegendPosition.Right || Legend.Position == LegendPosition.Left)
                {
                    if (Legend.Position == LegendPosition.Right)
                    {
                        MarginRight = legendSize + 16;
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

            CategoryScale.Output = new ScaleRange { Start = MarginLeft, End = Width.Value - MarginRight };
            ValueScale.Output = new ScaleRange { Start = Height.Value - MarginBottom, End = MarginTop };

            ValueScale.Fit(ValueAxis.TickDistance);
            CategoryScale.Fit(CategoryAxis.TickDistance);

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

                CategoryScale.Output = new ScaleRange { Start = MarginLeft, End = Width.Value - MarginRight };
            }

            if (height != Height)
            {
                Height = height;
                stateHasChanged = true;

                ValueScale.Output = new ScaleRange { Start = Height.Value - MarginBottom, End = MarginTop };
            }

            if (stateHasChanged)
            {
                await Refresh();
            }
        }

        RenderFragment tooltip;
        object tooltipData;
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
            IChartSeries closestSeries = null;
            object closestSeriesData = null;
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

            if (closestSeriesData != null)
            {
                await closestSeries.InvokeClick(SeriesClick, closestSeriesData);
            }
        }

        internal async Task DisplayTooltip()
        {
            if (Tooltip.Visible)
            {
                var orderedSeries = Series.OrderBy(s => s.RenderingOrder).Reverse();
                IChartSeries closestSeries = null;
                object closestSeriesData = null;
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
                                TooltipService.OpenChartTooltip(Element, tooltipPosition.X + MarginLeft, tooltipPosition.Y + MarginTop, _ => tooltip, new ChartTooltipOptions
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

                if (closestSeriesData != null)
                {
                    if (closestSeriesData != tooltipData)
                    {
                        tooltipData = closestSeriesData;
                        tooltip = closestSeries.RenderTooltip(closestSeriesData);
                        var tooltipPosition = closestSeries.GetTooltipPosition(closestSeriesData);
                        TooltipService.OpenChartTooltip(Element, tooltipPosition.X + MarginLeft, tooltipPosition.Y + MarginTop, _ => tooltip, new ChartTooltipOptions
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

                TooltipService.Close();
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

        private bool widthAndHeightAreSet = false;
        private bool firstRender = true;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
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

        internal string ClipPath { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ClipPath = $"clipPath{UniqueID}";
            CategoryAxis.Chart = this;
            ValueAxis.Chart = this;

            Initialize();
        }

        private void Initialize()
        {
            double width = 0;
            double height = 0;

            if (CurrentStyle.ContainsKey("height"))
            {
                var pixelHeight = CurrentStyle["height"];

                if (pixelHeight.EndsWith("px"))
                {
                    height = Convert.ToDouble(pixelHeight.TrimEnd("px".ToCharArray()));
                }
            }

            if (CurrentStyle.ContainsKey("width"))
            {
                var pixelWidth = CurrentStyle["width"];

                if (pixelWidth.EndsWith("px"))
                {
                    width = Convert.ToDouble(pixelWidth.TrimEnd("px".ToCharArray()));
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

        private bool visibleChanged = false;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldRefresh = parameters.DidParameterChange(nameof(Style), Style);

            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (shouldRefresh)
            {
                Initialize();
            }

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
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

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoid("Radzen.destroyChart", Element);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-chart rz-scheme-{ColorScheme.ToString().ToLowerInvariant()}";
        }
    }
}

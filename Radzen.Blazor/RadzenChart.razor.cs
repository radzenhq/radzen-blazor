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
    /// Displays line, area, donut, pie, bar or column series.
    /// </summary>
    /// <example>
    /// <code>
    ///   &lt;RadzenChart&gt;
    ///       &lt;RadzenColumnSeries Data=@revenue CategoryProperty="Quarter" ValueProperty="Revenue" /&gt;
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
    public partial class RadzenChart : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the color scheme used to render the series.
        /// </summary>
        /// <value>The color scheme.</value>
        [Parameter]
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks on a series. 
        /// </summary>
        [Parameter]
        public EventCallback<SeriesClickEventArgs> SeriesClick { get; set; }

        double? Width { get; set; }

        double? Height { get; set; }

        double MarginTop { get; set; }

        double MarginLeft { get; set; }

        double MarginRight { get; set; }

        double MarginBottom { get; set; }

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

        private bool ShouldRenderAxes()
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

        private bool UpdateScales()
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

            var stateHasChanged = false;

            if (!ValueScale.IsEqualTo(valueScale))
            {
                stateHasChanged = true;
            }

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
        /// Invoked via interop when the user clicks the RadzenChart. Raises the <see cref="SeriesClick" /> handler.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        [JSInvokable]
        public async Task Click(double x, double y)
        {
            foreach (var series in Series)
            {
                if (series.Visible && series.Contains(mouseX - MarginLeft, mouseY - MarginTop, 5))
                {
                    var data = series.DataAt(mouseX - MarginLeft, mouseY - MarginTop);

                    if (data != null)
                    {
                        await series.InvokeClick(SeriesClick, data);
                    }

                    return;
                }
            }
        }

        internal async Task DisplayTooltip()
        {
            if (Tooltip.Visible)
            {
                var orderedSeries = Series.OrderBy(s => s.RenderingOrder).Reverse();

                foreach (var series in orderedSeries)
                {
                    if (series.Visible && series.Contains(mouseX - MarginLeft, mouseY - MarginTop, 25))
                    {
                        var data = series.DataAt(mouseX - MarginLeft, mouseY - MarginTop);

                        if (data != tooltipData)
                        {
                            tooltipData = data;
                            tooltip = series.RenderTooltip(data, MarginLeft, MarginTop);
                            StateHasChanged();
                            await Task.Yield();
                        }

                        return;
                    }
                }

                if (tooltip != null)
                {
                    tooltipData = null;
                    tooltip = null;

                    StateHasChanged();
                    await Task.Yield();
                }
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
                JSRuntime.InvokeVoidAsync("Radzen.destroyChart", Element);
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-chart rz-scheme-{ColorScheme.ToString().ToLowerInvariant()}";
        }
    }
}

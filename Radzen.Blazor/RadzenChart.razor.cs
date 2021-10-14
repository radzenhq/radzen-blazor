using Microsoft.JSInterop;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenChart.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenChart : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the color scheme.
        /// </summary>
        /// <value>The color scheme.</value>
        [Parameter]
        public ColorScheme ColorScheme { get; set; }

        /// <summary>
        /// Gets or sets the series click.
        /// </summary>
        /// <value>The series click.</value>
        [Parameter]
        public EventCallback<SeriesClickEventArgs> SeriesClick { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        double? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        double? Height { get; set; }

        /// <summary>
        /// Gets or sets the margin top.
        /// </summary>
        /// <value>The margin top.</value>
        double MarginTop { get; set; }

        /// <summary>
        /// Gets or sets the margin left.
        /// </summary>
        /// <value>The margin left.</value>
        double MarginLeft { get; set; }

        /// <summary>
        /// Gets or sets the margin right.
        /// </summary>
        /// <value>The margin right.</value>
        double MarginRight { get; set; }

        /// <summary>
        /// Gets or sets the margin bottom.
        /// </summary>
        /// <value>The margin bottom.</value>
        double MarginBottom { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the category scale.
        /// </summary>
        /// <value>The category scale.</value>
        internal ScaleBase CategoryScale { get; set; } = new LinearScale();
        /// <summary>
        /// Gets or sets the value scale.
        /// </summary>
        /// <value>The value scale.</value>
        internal ScaleBase ValueScale { get; set; } = new LinearScale();
        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        /// <value>The series.</value>
        internal IList<IChartSeries> Series { get; set; } = new List<IChartSeries>();
        /// <summary>
        /// Gets or sets the column options.
        /// </summary>
        /// <value>The column options.</value>
        internal RadzenColumnOptions ColumnOptions { get; set; } = new RadzenColumnOptions();
        /// <summary>
        /// Gets or sets the bar options.
        /// </summary>
        /// <value>The bar options.</value>
        internal RadzenBarOptions BarOptions { get; set; } = new RadzenBarOptions();

        /// <summary>
        /// Gets or sets the legend.
        /// </summary>
        /// <value>The legend.</value>
        internal RadzenLegend Legend { get; set; } = new RadzenLegend();
        /// <summary>
        /// Gets or sets the category axis.
        /// </summary>
        /// <value>The category axis.</value>
        internal RadzenCategoryAxis CategoryAxis { get; set; } = new RadzenCategoryAxis();
        /// <summary>
        /// Gets or sets the value axis.
        /// </summary>
        /// <value>The value axis.</value>
        internal RadzenValueAxis ValueAxis { get; set; } = new RadzenValueAxis();
        /// <summary>
        /// Gets or sets the tooltip.
        /// </summary>
        /// <value>The tooltip.</value>
        internal RadzenChartTooltipOptions Tooltip { get; set; } = new RadzenChartTooltipOptions();

        /// <summary>
        /// Adds the series.
        /// </summary>
        /// <param name="series">The series.</param>
        internal void AddSeries(IChartSeries series)
        {
            if (!Series.Contains(series))
            {
                Series.Add(series);
            }
        }

        /// <summary>
        /// Removes the series.
        /// </summary>
        /// <param name="series">The series.</param>
        internal void RemoveSeries(IChartSeries series)
        {
            Series.Remove(series);
        }

        /// <summary>
        /// Shoulds the render axes.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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

        /// <summary>
        /// Shoulds the invert axes.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool ShouldInvertAxes()
        {
            return Series.Count > 0 && Series.All(series => series is IChartBarSeries);
        }

        /// <summary>
        /// Updates the scales.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
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
        /// Resizes the specified width.
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

        /// <summary>
        /// The tooltip
        /// </summary>
        RenderFragment tooltip;
        /// <summary>
        /// The tooltip data
        /// </summary>
        object tooltipData;
        /// <summary>
        /// The mouse x
        /// </summary>
        double mouseX;
        /// <summary>
        /// The mouse y
        /// </summary>
        double mouseY;

        /// <summary>
        /// Mouses the move.
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
        /// Clicks the specified x.
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

        /// <summary>
        /// Displays the tooltip.
        /// </summary>
        internal async Task DisplayTooltip()
        {
            if (Tooltip.Visible)
            {
                foreach (var series in Series)
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

        /// <summary>
        /// The width and height are set
        /// </summary>
        private bool widthAndHeightAreSet = false;
        /// <summary>
        /// The first render
        /// </summary>
        private bool firstRender = true;

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

                        Resize(rect.Width, rect.Height);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the clip path.
        /// </summary>
        /// <value>The clip path.</value>
        internal string ClipPath { get; set; }

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            ClipPath = $"clipPath{UniqueID}";
            CategoryAxis.Chart = this;
            ValueAxis.Chart = this;

            Initialize();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
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

        /// <summary>
        /// The visible changed
        /// </summary>
        private bool visibleChanged = false;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Refreshes the specified force.
        /// </summary>
        /// <param name="force">if set to <c>true</c> [force].</param>
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
        /// Reloads this instance.
        /// </summary>
        public async Task Reload()
        {
            await Refresh(true);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (Visible && IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyChart", Element);
            }
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return $"rz-chart rz-scheme-{ColorScheme.ToString().ToLower()}";
        }
    }
}
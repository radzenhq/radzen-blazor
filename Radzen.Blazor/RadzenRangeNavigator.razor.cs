using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// A standalone range navigator component that displays a mini-chart with a draggable selection window.
    /// Connect to a <see cref="RadzenChart" /> via two-way binding on <c>Start</c>/<c>End</c> and the chart's <c>ViewStart</c>/<c>ViewEnd</c>.
    /// </summary>
    public partial class RadzenRangeNavigator : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the start of the selected range as a fraction (0-1).
        /// Supports two-way binding with <c>@bind-Start</c>.
        /// </summary>
        [Parameter]
        public double Start { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the Start value changes due to user interaction.
        /// </summary>
        [Parameter]
        public EventCallback<double> StartChanged { get; set; }

        /// <summary>
        /// Gets or sets the end of the selected range as a fraction (0-1).
        /// Supports two-way binding with <c>@bind-End</c>.
        /// </summary>
        [Parameter]
        public double End { get; set; } = 1;

        /// <summary>
        /// Gets or sets the callback invoked when the End value changes due to user interaction.
        /// </summary>
        [Parameter]
        public EventCallback<double> EndChanged { get; set; }

        internal double Height { get; set; } = 80;

        /// <summary>
        /// Gets or sets whether labels are displayed above the selection handles showing the current range values.
        /// </summary>
        [Parameter]
        public bool ShowHandleLabels { get; set; }

        /// <summary>
        /// Gets or sets the format string for handle labels. Use standard .NET format strings, e.g. <c>"{0:MMM dd, yyyy}"</c> for dates.
        /// When not set, defaults to a short representation based on the data type.
        /// </summary>
        [Parameter]
        public string? HandleLabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets whether an axis with tick labels is displayed below the navigator.
        /// </summary>
        [Parameter]
        public bool ShowAxis { get; set; }

        /// <summary>
        /// Gets or sets the format string for axis tick labels.
        /// Use standard .NET format strings, e.g. <c>"{0:MMM yyyy}"</c> for dates or <c>"{0:N0}"</c> for numbers.
        /// When not set, defaults to a short representation based on the data type.
        /// </summary>
        [Parameter]
        public string? AxisFormatString { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for the axis. Use when there are no child series to define the range.
        /// Supports <see cref="DateTime"/> and numeric types.
        /// </summary>
        [Parameter]
        public object? Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for the axis. Use when there are no child series to define the range.
        /// Supports <see cref="DateTime"/> and numeric types.
        /// </summary>
        [Parameter]
        public object? Max { get; set; }

        /// <summary>
        /// Gets or sets the child content (navigator series).
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        internal IList<IRangeNavigatorSeries> NavigatorSeries { get; set; } = new List<IRangeNavigatorSeries>();

        internal ScaleBase CategoryScale { get; set; } = new LinearScale();
        internal ScaleBase ValueScale { get; set; } = new LinearScale();

        internal double Width { get; set; }

        private bool firstRender = true;
        private bool isDragging;

        /// <inheritdoc />
        protected override bool ShouldRender()
        {
            if (isDragging)
            {
                return false;
            }

            return base.ShouldRender();
        }

        internal void AddSeries(IRangeNavigatorSeries series)
        {
            if (!NavigatorSeries.Contains(series))
            {
                NavigatorSeries.Add(series);
            }
        }

        internal void RemoveSeries(IRangeNavigatorSeries series)
        {
            NavigatorSeries.Remove(series);
        }

        internal void UpdateScales()
        {
            if (Width <= 0)
            {
                return;
            }

            CategoryScale = new LinearScale();
            ValueScale = new LinearScale();

            foreach (var series in NavigatorSeries)
            {
                CategoryScale = series.TransformCategoryScale(CategoryScale);
                ValueScale = series.TransformValueScale(ValueScale);
            }

            if (NavigatorSeries.Count == 0 && Min != null && Max != null)
            {
                if (Min is DateTime)
                {
                    CategoryScale = new DateScale();
                }

                CategoryScale.Resize(Min, Max);
            }

            CategoryScale.Output = new ScaleRange { Start = 0, End = Width };
            ValueScale.Output = new ScaleRange { Start = Height, End = 0 };

            CategoryScale.Fit(10);
            ValueScale.Fit(10);

            UpdateJSLabelConfig();
        }

        internal IList<AxisTick> GetAxisTicks()
        {
            var ticks = new List<AxisTick>();

            if (CategoryScale?.Input == null || Width <= 0)
            {
                return ticks;
            }

            var inputStart = CategoryScale.Input.Start;
            var inputEnd = CategoryScale.Input.End;

            if (inputStart == inputEnd)
            {
                return ticks;
            }

            var tickDistance = Math.Max(80, (int)(Width / 8));
            CategoryScale.Step = null;
            var tickInfo = CategoryScale.Ticks(tickDistance);
            var step = tickInfo.Step;

            if (step <= 0)
            {
                return ticks;
            }

            for (var value = tickInfo.Start; value <= tickInfo.End; value += step)
            {
                var fraction = (value - inputStart) / (inputEnd - inputStart);

                if (fraction < -0.01 || fraction > 1.01)
                {
                    continue;
                }

                var tickValue = CategoryScale.Value(value);
                string label;

                if (!string.IsNullOrEmpty(AxisFormatString))
                {
                    label = CategoryScale.FormatTick(AxisFormatString, tickValue);
                }
                else
                {
                    label = CategoryScale.FormatTick("", tickValue);
                }

                ticks.Add(new AxisTick { Position = Math.Clamp(fraction, 0, 1), Label = label });
            }

            if (ticks.Count > 0)
            {
                ticks[0].CssClass = "rz-range-nav-tick-first";
                ticks[ticks.Count - 1].CssClass = "rz-range-nav-tick-last";
            }

            return ticks;
        }

        internal class AxisTick
        {
            public double Position { get; set; }
            public string Label { get; set; } = "";
            public string CssClass { get; set; } = "";
        }

        /// <summary>
        /// Called from JS when the navigator element is resized.
        /// </summary>
        [JSInvokable]
        public void OnResize(double width, double height)
        {
            if (width != Width || height != Height)
            {
                Width = width;
                Height = height;
                UpdateScales();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Called from JS when the user drags the selection window or handles.
        /// </summary>
        [JSInvokable]
        public async Task OnNavigatorDrag(double start, double end)
        {
            // Capture in locals — between the two awaits below, the parent
            // re-renders and SetParametersAsync overwrites the End property
            // with the old value before EndChanged can fire.
            var newStart = Math.Clamp(start, 0, 1);
            var newEnd = Math.Clamp(end, 0, 1);

            Start = newStart;
            End = newEnd;

            isDragging = true;

            try
            {
                await StartChanged.InvokeAsync(newStart);
                await EndChanged.InvokeAsync(newEnd);
            }
            finally
            {
                isDragging = false;
                StateHasChanged();
            }
        }

        private void UpdateJSLabelConfig()
        {
            if (!ShowHandleLabels || JSRuntime == null || firstRender)
            {
                return;
            }

            var isDate = CategoryScale is DateScale;
            var inputStart = CategoryScale?.Input?.Start ?? 0;
            var inputEnd = CategoryScale?.Input?.End ?? 0;

            try
            {
                JSRuntime.InvokeVoidAsync("Radzen.updateRangeNavigatorLabels", Element, isDate, inputStart, inputEnd);
            }
            catch
            {
                // Ignore errors
            }
        }

        internal string GetHandleLabel(double fraction)
        {
            if (CategoryScale?.Input == null)
            {
                return "";
            }

            var inputStart = CategoryScale.Input.Start;
            var inputEnd = CategoryScale.Input.End;

            if (inputStart == inputEnd)
            {
                return "";
            }

            var value = inputStart + fraction * (inputEnd - inputStart);

            if (CategoryScale is DateScale)
            {
                var date = new DateTime((long)value);
                var format = HandleLabelFormatString ?? "{0:MM/dd/yyyy}";
                return string.Format(CultureInfo.InvariantCulture, format, date);
            }

            var format2 = HandleLabelFormatString ?? "{0:N0}";
            return string.Format(CultureInfo.InvariantCulture, format2, value);
        }

        internal bool IsCompact => NavigatorSeries.Count == 0;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var css = "rz-range-nav";

            if (IsCompact)
            {
                css += " rz-range-nav-compact";
            }

            if (ShowAxis)
            {
                css += " rz-range-nav-has-axis";
            }

            return css;
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                this.firstRender = false;

                if (JSRuntime != null)
                {
                    var rect = await JSRuntime.InvokeAsync<double[]>("Radzen.createRangeNavigator", Element, Reference);
                    if (rect != null && rect.Length >= 2 && rect[0] > 0)
                    {
                        Width = rect[0];
                        Height = rect[1];
                        UpdateScales();
                        UpdateJSLabelConfig();
                        StateHasChanged();
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                try
                {
                    JSRuntime!.InvokeVoidAsync("Radzen.disposeElement", Element);
                }
                catch
                {
                    // Ignore errors during disposal
                }
            }
        }
    }
}

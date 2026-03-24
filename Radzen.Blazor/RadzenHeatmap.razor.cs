using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A standalone heatmap chart component that displays data as a color-coded grid.
    /// </summary>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenHeatmap : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the data items.
        /// </summary>
        [Parameter]
        public IEnumerable<object>? Data { get; set; }

        /// <summary>
        /// Gets or sets the property name for X-axis categories.
        /// </summary>
        [Parameter]
        public string? XProperty { get; set; }

        /// <summary>
        /// Gets or sets the property name for Y-axis categories.
        /// </summary>
        [Parameter]
        public string? YProperty { get; set; }

        /// <summary>
        /// Gets or sets the property name for cell values (determines color intensity).
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the color for the minimum value.
        /// </summary>
        [Parameter]
        public string MinColor { get; set; } = "#f0f0f0";

        /// <summary>
        /// Gets or sets the color for the maximum value.
        /// </summary>
        [Parameter]
        public string MaxColor { get; set; } = "#1a9641";

        /// <summary>
        /// Gets or sets the padding between cells in pixels.
        /// </summary>
        [Parameter]
        public double CellPadding { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to show values inside cells.
        /// </summary>
        [Parameter]
        public bool ShowValues { get; set; }

        /// <summary>
        /// Gets or sets the format string for cell values.
        /// </summary>
        [Parameter]
        public string? FormatString { get; set; }

        /// <summary>
        /// Gets or sets the child content (unused, for future extensibility).
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        private double? Width { get; set; }
        private double? Height { get; set; }
        private DotNetObjectReference<RadzenHeatmap>? reference;

        /// <summary>
        /// Resizes the heatmap.
        /// </summary>
        [JSInvokable("RadzenHeatmap.Resize")]
        public void Resize(double width, double height)
        {
            Width = width;
            Height = height;
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override async System.Threading.Tasks.Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                reference = DotNetObjectReference.Create(this);
                var rect = await JSRuntime!.InvokeAsync<Rect>("Radzen.createResizable", Element, reference);
                Width = rect.Width;
                Height = rect.Height;
                StateHasChanged();
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-heatmap";
        }

        private IList<object>? items;
        private IList<string>? xCategories;
        private IList<string>? yCategories;
        private double minValue;
        private double maxValue;

        private void ComputeLayout()
        {
            if (Data == null || string.IsNullOrEmpty(XProperty) || string.IsNullOrEmpty(YProperty) || string.IsNullOrEmpty(ValueProperty))
            {
                items = null;
                return;
            }

            items = Data.Cast<object>().ToList();

            xCategories = items.Select(i => PropertyAccess.GetValue(i, XProperty)?.ToString() ?? "").Distinct().ToList();
            yCategories = items.Select(i => PropertyAccess.GetValue(i, YProperty)?.ToString() ?? "").Distinct().ToList();

            var values = items.Select(i => Convert.ToDouble(PropertyAccess.GetValue(i, ValueProperty), System.Globalization.CultureInfo.InvariantCulture)).ToList();
            minValue = values.Min();
            maxValue = values.Max();
        }

        internal string InterpolateColor(double t)
        {
            var min = ParseColor(MinColor);
            var max = ParseColor(MaxColor);
            var r = (int)(min.r + (max.r - min.r) * t);
            var g = (int)(min.g + (max.g - min.g) * t);
            var b = (int)(min.b + (max.b - min.b) * t);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static (int r, int g, int b) ParseColor(string color)
        {
            color = color.TrimStart('#');
            if (color.Length == 6)
            {
                return (
                    Convert.ToInt32(color.Substring(0, 2), 16),
                    Convert.ToInt32(color.Substring(2, 2), 16),
                    Convert.ToInt32(color.Substring(4, 2), 16)
                );
            }
            return (240, 240, 240);
        }

        internal double GetValue(object item)
        {
            return Convert.ToDouble(PropertyAccess.GetValue(item, ValueProperty!), System.Globalization.CultureInfo.InvariantCulture);
        }

        internal string GetX(object item)
        {
            return PropertyAccess.GetValue(item, XProperty!)?.ToString() ?? "";
        }

        internal string GetY(object item)
        {
            return PropertyAccess.GetValue(item, YProperty!)?.ToString() ?? "";
        }

        internal string FormatValue(double value)
        {
            if (!string.IsNullOrEmpty(FormatString))
            {
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, FormatString, value);
            }
            return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
        }

        internal RenderFragment RenderLabels(double leftMargin, double topMargin, double bottomMargin, double cellWidth, double cellHeight)
        {
            return builder =>
            {
                var seq = 0;

                // Y-axis labels
                for (int yi = 0; yi < yCategories!.Count; yi++)
                {
                    var y = topMargin + yi * cellHeight + cellHeight / 2;
                    builder.OpenElement(seq++, "text");
                    builder.AddAttribute(seq++, "x", (leftMargin - 8).ToInvariantString());
                    builder.AddAttribute(seq++, "y", y.ToInvariantString());
                    builder.AddAttribute(seq++, "text-anchor", "end");
                    builder.AddAttribute(seq++, "dominant-baseline", "middle");
                    builder.AddAttribute(seq++, "style", "fill: var(--rz-text-color); font-size: 12px;");
                    builder.AddContent(seq++, yCategories[yi]);
                    builder.CloseElement();
                }

                // X-axis labels
                for (int xi = 0; xi < xCategories!.Count; xi++)
                {
                    var x = leftMargin + xi * cellWidth + cellWidth / 2;
                    var y = Height!.Value - bottomMargin + 20;
                    builder.OpenElement(seq++, "text");
                    builder.AddAttribute(seq++, "x", x.ToInvariantString());
                    builder.AddAttribute(seq++, "y", y.ToInvariantString());
                    builder.AddAttribute(seq++, "text-anchor", "middle");
                    builder.AddAttribute(seq++, "dominant-baseline", "middle");
                    builder.AddAttribute(seq++, "style", "fill: var(--rz-text-color); font-size: 12px;");
                    builder.AddContent(seq++, xCategories[xi]);
                    builder.CloseElement();
                }
            };
        }

        internal RenderFragment RenderSvgText(double x, double y, string textAnchor, string dominantBaseline, string style, string content)
        {
            return builder =>
            {
                builder.OpenElement(0, "text");
                builder.AddAttribute(1, "x", x.ToInvariantString());
                builder.AddAttribute(2, "y", y.ToInvariantString());
                builder.AddAttribute(3, "text-anchor", textAnchor);
                builder.AddAttribute(4, "dominant-baseline", dominantBaseline);
                builder.AddAttribute(5, "style", style);
                builder.AddContent(6, content);
                builder.CloseElement();
            };
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
                JSRuntime!.InvokeVoidAsync("Radzen.disposeElement", Element);
            }

            base.Dispose();
            reference?.Dispose();
        }
    }
}

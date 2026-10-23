using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a waterfall chart in a RadzenChart.
    /// Each column starts where the previous one ended, showing how sequential values contribute to a running total.
    /// Items with the summary property set to <c>true</c> render as total bars starting from zero.
    /// Positive values are colored with <see cref="PositiveFill"/>, negative with <see cref="NegativeFill"/>,
    /// and summary bars with <see cref="SummaryFill"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenWaterfallSeries Data=@data CategoryProperty="Label" ValueProperty="Amount"
    ///         SummaryProperty="IsTotal" Title="Cash Flow" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenWaterfallSeries<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        /// <summary>
        /// Gets or sets the fill color for positive value columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? PositiveFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for negative value columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? NegativeFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for summary (total) columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? SummaryFill { get; set; }

        /// <summary>
        /// Specifies how the series is filled. Set to <see cref="FillMode.Solid"/> by default.
        /// Use <see cref="FillMode.Gradient"/> for a fill that fades toward the axis baseline, or <see cref="FillMode.None"/> to render only the outline.
        /// </summary>
        /// <value>The fill mode. Default is <see cref="FillMode.Solid"/>.</value>
        [Parameter]
        public FillMode FillMode { get; set; } = FillMode.Solid;

        /// <summary>
        /// Specifies the opacity at the value end of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient start opacity. Default is <c>0.85</c>.</value>
        [Parameter]
        public double GradientStartOpacity { get; set; } = 0.85;

        /// <summary>
        /// Specifies the opacity at the baseline of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient end opacity. Default is <c>0.4</c>.</value>
        [Parameter]
        public double GradientEndOpacity { get; set; } = 0.4;

        /// <summary>
        /// Gets or sets the stroke (border) color for all columns.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the column border in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the line type for column borders.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the name of a boolean property on <typeparamref name="TItem"/> that indicates summary (total) items.
        /// Summary items render as bars starting from zero, representing the cumulative total up to that point.
        /// </summary>
        [Parameter]
        public string? SummaryProperty { get; set; }

        /// <inheritdoc />
        public override string Color => PositiveFill ?? string.Empty;

        int IChartColumnSeries.Count => Items?.Count ?? 0;

        internal Func<TItem, bool>? IsSummary
        {
            get
            {
                if (string.IsNullOrEmpty(SummaryProperty))
                {
                    return null;
                }

                return PropertyAccess.Getter<TItem, bool>(SummaryProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items == null || !Items.Any())
            {
                return scale;
            }

            var isSummary = IsSummary;
            double runningTotal = 0;
            double minValue = 0;
            double maxValue = 0;

            foreach (var item in Items)
            {
                var val = Value(item);
                var summary = isSummary?.Invoke(item) ?? false;

                if (summary)
                {
                    minValue = Math.Min(minValue, Math.Min(0, runningTotal));
                    maxValue = Math.Max(maxValue, Math.Max(0, runningTotal));
                }
                else
                {
                    var prevTotal = runningTotal;
                    runningTotal += val;
                    minValue = Math.Min(minValue, Math.Min(prevTotal, runningTotal));
                    maxValue = Math.Max(maxValue, Math.Max(prevTotal, runningTotal));
                }
            }

            scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            return scale;
        }

        internal IList<WaterfallItem> ComputeWaterfallItems()
        {
            var result = new List<WaterfallItem>();
            var isSummary = IsSummary;
            double runningTotal = 0;

            foreach (var item in Items)
            {
                var val = Value(item);
                var summary = isSummary?.Invoke(item) ?? false;

                if (summary)
                {
                    result.Add(new WaterfallItem { Start = 0, End = runningTotal, IsSummary = true });
                }
                else
                {
                    var start = runningTotal;
                    runningTotal += val;
                    result.Add(new WaterfallItem { Start = start, End = runningTotal, IsSummary = false });
                }
            }

            return result;
        }

        internal class WaterfallItem
        {
            public double Start { get; set; }
            public double End { get; set; }
            public bool IsSummary { get; set; }
        }

        private BandSlot Slot => CategoryBands.Columns(RequireChart(), this);

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var index = Items.IndexOf(item);
            if (index < 0)
            {
                return style;
            }

            var wfItems = ComputeWaterfallItems();
            var wf = wfItems[index];
            var color = wf.IsSummary ? SummaryFill : (wf.End >= wf.Start ? PositiveFill : NegativeFill);

            if (color != null)
            {
                return $"{style}; border-color: {color};";
            }

            return style;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var chart = RequireChart();
            var category = ComposeCategory(chart.CategoryScale);

            return Slot.Center(category(item));
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var index = Items.IndexOf(item);
            if (index < 0)
            {
                return 0;
            }

            var wfItems = ComputeWaterfallItems();
            var wf = wfItems[index];
            return chart.ValueScale.Scale(wf.End, true);
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            var chart = Chart;
            if (chart == null)
            {
                return (default!, new Point());
            }

            var category = ComposeCategory(chart.CategoryScale);
            var slot = Slot;
            var width = slot.Size;

            var wfItems = ComputeWaterfallItems();

            for (var i = 0; i < Items.Count; i++)
            {
                var data = Items[i];
                var wf = wfItems[i];

                var startX = slot.Start(category(data));
                var endX = startX + width;
                var y1 = chart.ValueScale.Scale(wf.Start, true);
                var y2 = chart.ValueScale.Scale(wf.End, true);
                var startY = Math.Min(y1, y2);
                var endY = Math.Max(y1, y2);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data!, new Point { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }
    }
}

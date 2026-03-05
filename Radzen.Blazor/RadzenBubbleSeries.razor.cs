using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that extends <see cref="RadzenScatterSeries{TItem}"/> to add a third data dimension
    /// mapped to circle radius. Each point is rendered as a circle whose area is proportional to the
    /// <see cref="SizeProperty"/> value.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenBubbleSeries Data=@data CategoryProperty="GDP" ValueProperty="LifeExpectancy"
    ///         SizeProperty="Population" Title="Countries" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenBubbleSeries<TItem> : RadzenScatterSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the property name for the size dimension. Must be numeric.
        /// </summary>
        [Parameter]
        public string? SizeProperty { get; set; }

        /// <summary>
        /// Gets or sets the minimum circle radius in pixels.
        /// </summary>
        [Parameter]
        public double MinSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum circle radius in pixels.
        /// </summary>
        [Parameter]
        public double MaxSize { get; set; } = 40;

        internal Func<TItem, double> Size
        {
            get
            {
                if (string.IsNullOrEmpty(SizeProperty))
                {
                    return _ => 1;
                }

                return PropertyAccess.Getter<TItem, double>(SizeProperty);
            }
        }

        internal double GetBubbleRadius(TItem item, double minValue, double maxValue)
        {
            var sizeValue = Math.Abs(Size(item));
            double normalized;

            if (maxValue == minValue)
            {
                normalized = 0.5;
            }
            else
            {
                normalized = (sizeValue - minValue) / (maxValue - minValue);
            }

            return Math.Sqrt(normalized) * (MaxSize - MinSize) / 2 + MinSize / 2;
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            if (base.Contains(x, y, tolerance))
            {
                return true;
            }

            var chart = Chart;
            if (chart == null || !Items.Any())
            {
                return false;
            }

            var category = ComposeCategory(chart.CategoryScale);
            var value = ComposeValue(chart.ValueScale);
            var sizeValues = Items.Select(item => Math.Abs(Size(item))).ToList();
            var minValue = sizeValues.Min();
            var maxValue = sizeValues.Max();

            return Items.Any(item =>
            {
                var px = category(item);
                var py = value(item);
                var radius = GetBubbleRadius(item, minValue, maxValue);
                var dx = px - x;
                var dy = py - y;
                return Math.Sqrt(dx * dx + dy * dy) <= radius;
            });
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            var yValue = base.TooltipValue(item);

            if (!string.IsNullOrEmpty(SizeProperty))
            {
                var sizeValue = Size(item);
                return $"{yValue} | Size: {sizeValue:N0}";
            }

            return yValue;
        }
    }
}

using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Common configuration of <see cref="RadzenBarSeries{TItem}" />.
    /// </summary>
    public partial class RadzenBarOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the border radius of the bars. 
        /// </summary>
        /// <value>The radius. Values greater than <c>0</c> make rounded corners.</value>
        [Parameter]
        public double Radius { get; set; }

        /// <summary>
        /// Gets or sets the margin between bars.
        /// </summary>
        /// <value>The margin. By default set to <c>10</c></value>
        [Parameter]
        public double Margin { get; set; } = 10;

        /// <summary>
        /// Gets or sets the height of all bars in pixels. By default it is automatically calculated depending on the chart height.
        /// </summary>
        /// <value>The pixel height of the bar. By default set to <c>null</c></value>
        [Parameter]
        public double? Height { get; set;}

        /// <summary>
        /// Gets or sets the maximum height of a bar in pixels. When the automatically calculated height
        /// exceeds this value the bars are capped to it and stay centered on their category. Has no
        /// effect when <see cref="Height" /> is set.
        /// </summary>
        /// <value>The maximum pixel height of a bar. By default set to <c>null</c> (no cap).</value>
        [Parameter]
        public double? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the fraction (0 to 1) of each category band left empty as a gap, controlling how
        /// thick the bars are relative to the space available per category. For example <c>0.4</c> makes
        /// the bars occupy 60% of the band. When <c>null</c> (the default) the height is derived from the
        /// chart size automatically. Has no effect when <see cref="Height" /> is set.
        /// </summary>
        /// <value>The category gap as a fraction between 0 and 1. By default set to <c>null</c>.</value>
        [Parameter]
        public double? CategoryGap { get; set; }

        /// <summary>
        /// The maximum bar height that actually applies. <see cref="MaxHeight" /> only caps the
        /// automatically calculated height, so it is ignored when an explicit <see cref="Height" /> is set.
        /// </summary>
        internal double? EffectiveMaxHeight => Height.HasValue ? null : MaxHeight;

        /// <inheritdoc />
        protected override void Initialize()
        {
            if (Chart != null)
            {
                Chart.BarOptions = this;
            }
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Radius), Radius) ||
                   DidParameterChange(parameters, nameof(Height), Height) ||
                   DidParameterChange(parameters, nameof(MaxHeight), MaxHeight) ||
                   DidParameterChange(parameters, nameof(CategoryGap), CategoryGap) ||
                   DidParameterChange(parameters, nameof(Margin), Margin);
        }
    }
}
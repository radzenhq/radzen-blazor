using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Common configuration of <see cref="RadzenColumnSeries{TItem}" />.
    /// </summary>
    public partial class RadzenColumnOptions : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the border radius of the bars. 
        /// </summary>
        /// <value>The radius. Values greater than <c>0</c> make rounded corners.</value>
        [Parameter]
        public double Radius { get; set; }

        /// <summary>
        /// Gets or sets the margin between columns.
        /// </summary>
        /// <value>The margin. By default set to <c>10</c></value>
        [Parameter]
        public double Margin { get; set; } = 10;

        /// <summary>
        /// Gets or sets the width of all columns in pixels. By default it is automatically calculated depending on the chart width.
        /// </summary>
        /// <value>The pixel width of the column. By default set to <c>null</c></value>
        [Parameter]
        public double? Width { get; set;}

        /// <summary>
        /// Gets or sets the maximum width of a column in pixels. When the automatically calculated width
        /// exceeds this value the columns are capped to it and stay centered on their category. Has no
        /// effect when <see cref="Width" /> is set.
        /// </summary>
        /// <value>The maximum pixel width of a column. By default set to <c>null</c> (no cap).</value>
        [Parameter]
        public double? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the fraction (0 to 1) of each category band left empty as a gap, controlling how
        /// wide the columns are relative to the space available per category. For example <c>0.4</c>
        /// makes the columns occupy 60% of the band. When <c>null</c> (the default) the width is derived
        /// from the chart size automatically. Has no effect when <see cref="Width" /> is set.
        /// </summary>
        /// <value>The category gap as a fraction between 0 and 1. By default set to <c>null</c>.</value>
        [Parameter]
        public double? CategoryGap { get; set; }

        /// <summary>
        /// The maximum column width that actually applies. <see cref="MaxWidth" /> only caps the
        /// automatically calculated width, so it is ignored when an explicit <see cref="Width" /> is set.
        /// </summary>
        internal double? EffectiveMaxWidth => Width.HasValue ? null : MaxWidth;

        /// <inheritdoc />
        protected override void Initialize()
        {
            if (Chart != null)
            {
                Chart.ColumnOptions = this;
            }
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Radius), Radius) ||
                   DidParameterChange(parameters, nameof(Width), Width) ||
                   DidParameterChange(parameters, nameof(MaxWidth), MaxWidth) ||
                   DidParameterChange(parameters, nameof(CategoryGap), CategoryGap) ||
                   DidParameterChange(parameters, nameof(Margin), Margin);
        }
    }
}

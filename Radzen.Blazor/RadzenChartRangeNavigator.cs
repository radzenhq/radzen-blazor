using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Attaches a <see cref="RadzenRangeNavigator" /> below a <see cref="RadzenChart" />, automatically bound to the chart's view range.
    /// Dragging the navigator window zooms the chart; zooming or panning the chart moves the navigator window. No manual binding is required.
    /// Declare <see cref="RadzenRangeNavigatorLineSeries{TItem}" /> children to display a data preview, or leave empty for a compact navigator.
    /// </summary>
    /// <example>
    /// <code>
    ///   &lt;RadzenChart AllowZoom="true" AllowPan="true"&gt;
    ///       &lt;RadzenLineSeries Data=@data CategoryProperty="Date" ValueProperty="Value" /&gt;
    ///       &lt;RadzenChartRangeNavigator&gt;
    ///           &lt;RadzenRangeNavigatorLineSeries Data=@data CategoryProperty="Date" ValueProperty="Value" /&gt;
    ///       &lt;/RadzenChartRangeNavigator&gt;
    ///   &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenChartRangeNavigator : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the navigator is displayed. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the height of the navigator in pixels.
        /// </summary>
        /// <value>The height in pixels. Default is <c>64</c>.</value>
        [Parameter]
        public double Height { get; set; } = 64;

        /// <summary>
        /// Gets or sets a value indicating whether labels with the current range values are displayed next to the drag handles.
        /// </summary>
        /// <value><c>true</c> to show handle labels; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ShowHandleLabels { get; set; }

        /// <summary>
        /// Gets or sets the format string used to format the handle labels.
        /// </summary>
        /// <value>The handle label format string.</value>
        [Parameter]
        public string? HandleLabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the child content. Used to declare <see cref="RadzenRangeNavigatorLineSeries{TItem}" /> preview series.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <inheritdoc />
        protected override void Initialize()
        {
            if (Chart != null)
            {
                Chart.RangeNavigator = this;
            }
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return parameters.DidParameterChange(nameof(Visible), Visible)
                || parameters.DidParameterChange(nameof(Height), Height)
                || parameters.DidParameterChange(nameof(ShowHandleLabels), ShowHandleLabels)
                || parameters.DidParameterChange(nameof(HandleLabelFormatString), HandleLabelFormatString);
        }
    }
}

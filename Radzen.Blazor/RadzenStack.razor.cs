using Microsoft.AspNetCore.Components;
using System;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A flexbox container component that arranges child elements in a vertical or horizontal stack with configurable spacing and alignment.
    /// RadzenStack provides a simpler alternative to RadzenRow/RadzenColumn for linear layouts without the 12-column grid constraint.
    /// Ideal for creating simple vertical or horizontal layouts without needing a grid system. Unlike RadzenRow/RadzenColumn which uses a 12-column grid, Stack arranges children linearly with equal spacing.
    /// Features Vertical (column) or Horizontal (row) orientation, consistent gap spacing between child elements, AlignItems for cross-axis alignment and JustifyContent for main-axis distribution,
    /// option to reverse the order of children, and control whether children wrap to new lines or stay in a single line.
    /// Use for simpler layouts like button groups, form field stacks, or toolbar arrangements.
    /// </summary>
    /// <example>
    /// Vertical stack with gap spacing:
    /// <code>
    /// &lt;RadzenStack Gap="1rem"&gt;
    ///     &lt;RadzenText&gt;First item&lt;/RadzenText&gt;
    ///     &lt;RadzenText&gt;Second item&lt;/RadzenText&gt;
    ///     &lt;RadzenText&gt;Third item&lt;/RadzenText&gt;
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// Horizontal button group:
    /// <code>
    /// &lt;RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" JustifyContent="JustifyContent.End"&gt;
    ///     &lt;RadzenButton Text="Cancel" /&gt;
    ///     &lt;RadzenButton Text="Save" ButtonStyle="ButtonStyle.Primary" /&gt;
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// Centered content with wrapping:
    /// <code>
    /// &lt;RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" AlignItems="AlignItems.Center" Gap="2rem"&gt;
    ///     @* Child elements *@
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// </example>
    public partial class RadzenStack : RadzenFlexComponent
    {
        /// <summary>
        /// Gets or sets the flex wrap behavior controlling whether child elements wrap to new lines when they don't fit.
        /// NoWrap keeps all children on one line (may cause overflow), Wrap allows wrapping to multiple lines.
        /// </summary>
        /// <value>The flex wrap mode. Default is <see cref="FlexWrap.NoWrap"/>.</value>
        [Parameter]
        public FlexWrap Wrap { get; set; } = FlexWrap.NoWrap;

        /// <summary>
        /// Gets or sets the stack direction: Vertical arranges children top-to-bottom, Horizontal arranges left-to-right.
        /// This determines the main axis direction for the flexbox layout.
        /// </summary>
        /// <value>The stack orientation. Default is <see cref="Orientation.Vertical"/>.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets the spacing between child elements in the stack.
        /// Accepts CSS length values (e.g., "1rem", "16px", "2em") or unitless numbers (interpreted as pixels).
        /// The gap applies uniformly between all adjacent children.
        /// </summary>
        /// <value>The gap spacing as a CSS length value. Default is null (no gap).</value>
        [Parameter]
        public string Gap { get; set; }

        /// <summary>
        /// Gets or sets whether to reverse the display order of child elements.
        /// When true, children are displayed in reverse order (bottom-to-top for vertical, right-to-left for horizontal).
        /// Useful for visual reordering without changing markup order.
        /// </summary>
        /// <value><c>true</c> to reverse child order; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Reverse { get; set; }

        /// <summary>
        /// Gets the final CSS style rendered by the component. Combines it with a <c>style</c> custom attribute.
        /// </summary>
        protected string GetStyle()
        {
            if (Attributes != null && Attributes.TryGetValue("style", out var style) && !string.IsNullOrEmpty(Convert.ToString(@style)))
            {
                return $"{GetComponentStyle()} {@style}";
            }

            return GetComponentStyle();
        }

        /// <summary>
        /// Gets the component CSS style.
        /// </summary>
        protected string GetComponentStyle()
        {
            var wrap = "";

            if (Wrap == FlexWrap.Wrap)
            {
                wrap = "flex-wrap:wrap;";
            }
            else if (Wrap == FlexWrap.NoWrap)
            {
                wrap = "flex-wrap:nowrap;";
            }
            else if (Wrap == FlexWrap.WrapReverse)
            {
                wrap = "flex-wrap:wrap-reverse;";
            }

            return $"{Style}{(!string.IsNullOrEmpty(Style) && !Style.EndsWith(";") ? ";" : "")}{(!string.IsNullOrEmpty(Gap) ? "--rz-gap:" + Gap + (Gap.All(c => Char.IsDigit(c)) ? "px;" : ";") : "")}{wrap}";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var horizontal = Orientation == Orientation.Horizontal;

            return $"rz-stack rz-display-flex rz-flex-{(horizontal ? "row" : "column")}{(Reverse ? "-reverse" : "")} rz-align-items-{GetFlexCSSClass<AlignItems>(AlignItems)} rz-justify-content-{GetFlexCSSClass<JustifyContent>(JustifyContent)}";
        }
    }
}

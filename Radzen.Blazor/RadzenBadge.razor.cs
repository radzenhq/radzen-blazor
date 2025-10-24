using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A small label component used to display counts, statuses, or short text labels with semantic color coding.
    /// RadzenBadge is commonly used for notification counts, status indicators, tags, or highlighting important information.
    /// </summary>
    /// <remarks>
    /// Badges are compact visual indicators that draw attention to specific information or states.
    /// The component supports:
    /// - **Styles**: Primary, Secondary, Success, Info, Warning, Danger, Light, Dark for semantic coloring
    /// - **Variants**: Filled (solid background), Flat (subtle background), Outlined (border only), Text (minimal styling)
    /// - **Shapes**: Standard rectangular or pill-shaped (rounded ends) via IsPill property
    /// - **Content**: Simple text via Text property or custom content via ChildContent
    /// - **Positioning**: Can be absolutely positioned to overlay other elements (e.g., notification icon with count)
    /// 
    /// Badges are often used inline with text, on buttons (to show counts), or overlaid on icons (notification badges).
    /// </remarks>
    /// <example>
    /// Basic badge with text:
    /// <code>
    /// &lt;RadzenBadge BadgeStyle="BadgeStyle.Primary" Text="New" /&gt;
    /// </code>
    /// Notification count badge:
    /// <code>
    /// &lt;div style="position: relative; display: inline-block;"&gt;
    ///     &lt;RadzenIcon Icon="notifications" /&gt;
    ///     &lt;RadzenBadge BadgeStyle="BadgeStyle.Danger" Text="3" IsPill="true" 
    ///                  Style="position: absolute; top: -8px; right: -8px;" /&gt;
    /// &lt;/div&gt;
    /// </code>
    /// Status badge with custom variant:
    /// <code>
    /// &lt;RadzenBadge BadgeStyle="BadgeStyle.Success" Variant="Variant.Flat" Text="Active" IsPill="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenBadge : RadzenComponent
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-badge")
                                                                     .Add($"rz-badge-{BadgeStyle.ToString().ToLowerInvariant()}")
                                                                     .AddVariant(Variant)
                                                                     .AddShade(Shade)
                                                                     .Add("rz-badge-pill", IsPill)
                                                                     .ToString();

        /// <summary>
        /// Gets or sets the custom child content to render inside the badge.
        /// When set, overrides the <see cref="Text"/> property for displaying custom markup.
        /// </summary>
        /// <value>The child content render fragment.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the text content displayed in the badge.
        /// Typically used for short text like numbers, single words, or abbreviations.
        /// </summary>
        /// <value>The badge text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the semantic color style of the badge.
        /// Determines the badge's color based on its purpose (Primary, Success, Danger, Warning, etc.).
        /// </summary>
        /// <value>The badge style. Default is <see cref="BadgeStyle.Primary"/>.</value>
        [Parameter]
        public BadgeStyle BadgeStyle { get; set; }

        /// <summary>
        /// Gets or sets the design variant that controls the badge's visual appearance.
        /// Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling).
        /// </summary>
        /// <value>The badge variant. Default is <see cref="Variant.Filled"/>.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

         /// <summary>
        /// Gets or sets the color intensity shade for the badge.
        /// Works in combination with <see cref="BadgeStyle"/> to adjust the color darkness/lightness.
        /// </summary>
        /// <value>The color shade. Default is <see cref="Shade.Default"/>.</value>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets whether the badge should have rounded pill-shaped ends instead of rectangular corners.
        /// Pill badges have a more modern, capsule-like appearance and are often used for tags or status indicators.
        /// </summary>
        /// <value><c>true</c> for pill shape; <c>false</c> for rectangular. Default is <c>false</c>.</value>
        [Parameter]
        public bool IsPill { get; set; }
    }
}
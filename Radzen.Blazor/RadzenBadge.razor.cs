using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenBadge component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenBadge BadgeStyle="BadgeStyle.Primary" Text="Primary" /&gt;
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
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the badge style.
        /// </summary>
        /// <value>The badge style.</value>
        [Parameter]
        public BadgeStyle BadgeStyle { get; set; }

        /// <summary>
        /// Gets or sets the badge variant.
        /// </summary>
        /// <value>The badge variant.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

         /// <summary>
        /// Gets or sets the badge shade color.
        /// </summary>
        /// <value>The badge shade color.</value>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pill.
        /// </summary>
        /// <value><c>true</c> if this instance is pill; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsPill { get; set; }
    }
}
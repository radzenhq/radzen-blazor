using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

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
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>();

            classList.Add("rz-badge");
            classList.Add($"rz-badge-{BadgeStyle.ToString().ToLowerInvariant()}");

            if (IsPill)
            {
                classList.Add("rz-badge-pill");
            }

            return string.Join(" ", classList);
        }

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
        /// Gets or sets a value indicating whether this instance is pill.
        /// </summary>
        /// <value><c>true</c> if this instance is pill; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsPill { get; set; }
    }
}

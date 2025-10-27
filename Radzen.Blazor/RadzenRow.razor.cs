using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// A flexbox row container component that horizontally arranges RadzenColumn components in a responsive 12-column grid layout.
    /// RadzenRow provides gap spacing, alignment, and justification controls for creating flexible, responsive page layouts.
    /// Serves as a container for RadzenColumn components, creating a horizontal flexbox layout where columns automatically wrap to the next line when their combined Size values exceed 12.
    /// Supports Gap and RowGap properties for spacing between columns and wrapped rows, AlignItems for vertical alignment (start, center, end, stretch, baseline),
    /// JustifyContent for horizontal distribution (start, center, end, space-between, space-around), and works seamlessly with RadzenColumn's breakpoint-specific sizing.
    /// Use AlignItems and JustifyContent from the base RadzenFlexComponent to control layout behavior.
    /// </summary>
    /// <example>
    /// Basic row with columns and gap:
    /// <code>
    /// &lt;RadzenRow Gap="1rem"&gt;
    ///     &lt;RadzenColumn Size="4"&gt;Column 1&lt;/RadzenColumn&gt;
    ///     &lt;RadzenColumn Size="4"&gt;Column 2&lt;/RadzenColumn&gt;
    ///     &lt;RadzenColumn Size="4"&gt;Column 3&lt;/RadzenColumn&gt;
    /// &lt;/RadzenRow&gt;
    /// </code>
    /// Row with alignment and justification:
    /// <code>
    /// &lt;RadzenRow AlignItems="AlignItems.Center" JustifyContent="JustifyContent.SpaceBetween" Gap="2rem"&gt;
    ///     &lt;RadzenColumn Size="3"&gt;Left&lt;/RadzenColumn&gt;
    ///     &lt;RadzenColumn Size="3"&gt;Right&lt;/RadzenColumn&gt;
    /// &lt;/RadzenRow&gt;
    /// </code>
    /// </example>
    public partial class RadzenRow : RadzenFlexComponent
    {
        /// <summary>
        /// Gets or sets the spacing between columns within the row.
        /// Accepts CSS length values (e.g., "1rem", "16px", "2em") or unitless numbers (interpreted as pixels).
        /// This sets the horizontal gap between column elements.
        /// </summary>
        /// <value>The gap spacing as a CSS length value. Default is null (no gap).</value>
        [Parameter]
        public string Gap { get; set; }

        /// <summary>
        /// Gets or sets the vertical spacing between wrapped rows when columns wrap to multiple lines.
        /// Accepts CSS length values (e.g., "1rem", "16px", "2em") or unitless numbers (interpreted as pixels).
        /// Only applicable when columns wrap due to exceeding the 12-column limit.
        /// </summary>
        /// <value>The row gap spacing as a CSS length value. Default is null (no row gap).</value>
        [Parameter]
        public string RowGap { get; set; }

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
            var list = new List<string>();

            if (!string.IsNullOrEmpty(Gap))
            {
                list.Add($"--rz-gap:{(Gap.All(char.IsDigit) ? Gap + "px" : Gap)}");
            }

            if (!string.IsNullOrEmpty(RowGap))
            {
                list.Add($"--rz-row-gap:{(RowGap.All(char.IsDigit) ? RowGap + "px" : RowGap)}");
            }

            return $"{Style}{(!string.IsNullOrEmpty(Style) && !Style.EndsWith(";") ? ";" : "")}{string.Join(";", list)}";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-display-flex rz-row rz-align-items-{GetFlexCSSClass<AlignItems>(AlignItems)} rz-justify-content-{GetFlexCSSClass<JustifyContent>(JustifyContent)}";
        }
    }
}

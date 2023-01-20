using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenRow : RadzenFlexComponent
    {
        /// <summary>
        /// Gets or sets the gap.
        /// </summary>
        /// <value>The gap.</value>
        [Parameter]
        public string Gap { get; set; }

        /// <summary>
        /// Gets or sets the row gap.
        /// </summary>
        /// <value>The row gap.</value>
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

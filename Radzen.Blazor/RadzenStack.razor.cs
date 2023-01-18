using Microsoft.AspNetCore.Components;
using System;
using System.Linq;
using System.Text;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenStack : RadzenFlexComponent
    {
        /// <summary>
        /// Gets or sets the wrap.
        /// </summary>
        /// <value>The wrap.</value>
        [Parameter]
        public FlexWrap Wrap { get; set; } = FlexWrap.NoWrap;

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets or sets the spacing
        /// </summary>
        /// <value>The spacing.</value>
        [Parameter]
        public string Gap { get; set; }

        /// <summary>
        /// Gets or sets the reverse
        /// </summary>
        /// <value>The reverse.</value>
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
                wrap = ";flex-wrap:wrap;";
            }
            else if (Wrap == FlexWrap.NoWrap)
            {
                wrap = ";flex-wrap:nowrap;";
            } 
            else if (Wrap == FlexWrap.WrapReverse)
            {
                wrap = ";flex-wrap:wrap-reverse;";
            }

            return $"{Style}{(!string.IsNullOrEmpty(Style) && !Style.EndsWith(";") ? ";" : "")}{(!string.IsNullOrEmpty(Gap) ? "--rz-gap:" + Gap + (Gap.All(c => Char.IsDigit(c)) ? "px;" : "") : "")}{wrap}";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var horizontal = Orientation == Orientation.Horizontal;

            return $"rz-stack rz-display-flex rz-flex-{(horizontal ? "row" : "column")}{(Reverse ? "-reverse" : "")} rz-align-items-{GetFlexCSSClass<AlignItems>(AlignItems)} rz-justify-content-{GetFlexCSSClass<JustifyContent>(JustifyContent)}";
        }
    }
}

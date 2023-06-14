using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenCard : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>();
            classList.Add("rz-card");
            classList.Add($"rz-variant-{Variant.ToString().ToLowerInvariant()}");

            return string.Join(" ", classList);
        }

        /// <summary>
        /// Gets or sets the card variant.
        /// </summary>
        /// <value>The card variant.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;
    }
}
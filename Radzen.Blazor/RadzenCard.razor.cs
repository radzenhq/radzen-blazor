using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenCard : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-card")
                                                                     .AddVariant(Variant)
                                                                     .ToString();

        /// <summary>
        /// Gets or sets the card variant.
        /// </summary>
        /// <value>The card variant.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;
    }
}
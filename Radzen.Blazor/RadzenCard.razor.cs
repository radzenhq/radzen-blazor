using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A card container component that groups related content with a consistent visual design and optional elevation.
    /// RadzenCard provides a versatile styled container for displaying information, images, actions, and other content in a structured format.
    /// Supports different visual variants (Filled, Flat, Outlined, Text) that affect the card's appearance.
    /// Works well in grid layouts (using RadzenRow/RadzenColumn) or can be stacked vertically.
    /// Ideal for grouping related information, creating dashboard widgets, displaying product information, or organizing form sections.
    /// Combine with other Radzen components like RadzenImage, RadzenText, and RadzenButton for rich card content.
    /// </summary>
    /// <example>
    /// Basic card with content:
    /// <code>
    /// &lt;RadzenCard&gt;
    ///     &lt;RadzenText TextStyle="TextStyle.H6"&gt;Card Title&lt;/RadzenText&gt;
    ///     &lt;RadzenText&gt;Card content goes here...&lt;/RadzenText&gt;
    /// &lt;/RadzenCard&gt;
    /// </code>
    /// Card with custom variant:
    /// <code>
    /// &lt;RadzenCard Variant="Variant.Outlined" Style="padding: 2rem;"&gt;
    ///     &lt;RadzenImage Path="product.jpg" Style="width: 100%; height: 200px; object-fit: cover;" /&gt;
    ///     &lt;RadzenText TextStyle="TextStyle.H5"&gt;Product Name&lt;/RadzenText&gt;
    ///     &lt;RadzenText&gt;Product description...&lt;/RadzenText&gt;
    ///     &lt;RadzenButton Text="Buy Now" ButtonStyle="ButtonStyle.Primary" /&gt;
    /// &lt;/RadzenCard&gt;
    /// </code>
    /// </example>
    public partial class RadzenCard : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-card")
                                                                     .AddVariant(Variant)
                                                                     .ToString();

        /// <summary>
        /// Gets or sets the visual design variant of the card.
        /// Controls the card's appearance: Filled (solid background with elevation), Flat (subtle background), 
        /// Outlined (border only), or Text (minimal styling).
        /// </summary>
        /// <value>The card variant. Default is <see cref="Variant.Filled"/>.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;
    }
}
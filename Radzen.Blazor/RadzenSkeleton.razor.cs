using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSkeleton component. Displays a loading placeholder with various animation types.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenSkeleton Variant="SkeletonVariant.Text" Animation="SkeletonAnimation.Wave" Style="width: 200px; height: 20px;" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSkeleton : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the type of skeleton shape.
        /// </summary>
        /// <value>The type.</value>
        [Parameter]
        public SkeletonVariant Variant { get; set; } = SkeletonVariant.Text;

        /// <summary>
        /// Gets or sets the animation type for the skeleton.
        /// </summary>
        /// <value>The animation.</value>
        [Parameter]
        public SkeletonAnimation Animation { get; set; } = SkeletonAnimation.None;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-skeleton")
                           .Add($"rz-skeleton-{Variant.ToString().ToLowerInvariant()}")
                           .Add($"rz-skeleton-{Animation.ToString().ToLowerInvariant()}", Animation != SkeletonAnimation.None)
                           .ToString();
        }

        /// <summary>
        /// Gets the final style string including component-specific styles.
        /// </summary>
        /// <returns>The style string.</returns>
        protected string GetStyle()
        {
            return Style;
        }
    }
} 
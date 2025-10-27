using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// An image display component that renders images from various sources including URLs, base64 data, or application assets.
    /// RadzenImage provides a simple wrapper for HTML img elements with click event support and alternate text for accessibility.
    /// Can display images from file paths (relative or absolute URLs to image files), external URLs (full HTTP/HTTPS URLs to remote images),
    /// base64 data (data URLs with embedded image data, e.g., from file uploads or database BLOBs), and application assets (images from wwwroot or other application folders).
    /// Use AlternateText to provide descriptive text for screen readers and when images fail to load.
    /// The Click event can be used to make images interactive (e.g., opening lightboxes or navigating).
    /// Combine with CSS (via Style or class attributes) for sizing, borders, shadows, and other visual effects.
    /// </summary>
    /// <example>
    /// Basic image from file:
    /// <code>
    /// &lt;RadzenImage Path="images/logo.png" AlternateText="Company Logo" Style="width: 200px;" /&gt;
    /// </code>
    /// Image with click handler:
    /// <code>
    /// &lt;RadzenImage Path=@product.ImageUrl AlternateText=@product.Name Click=@(args => ShowImageGallery(product)) Style="cursor: pointer;" /&gt;
    /// </code>
    /// Image from base64 data:
    /// <code>
    /// &lt;RadzenImage Path=@($"data:image/jpeg;base64,{base64String}") AlternateText="Uploaded Photo" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenImage : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the image source path or URL.
        /// Supports file paths (relative or absolute), external URLs, or data URLs with base64-encoded images.
        /// </summary>
        /// <value>The image source path, URL, or data URL.</value>
        [Parameter]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the alternate text describing the image for accessibility and when the image fails to load.
        /// This text is read by screen readers and displayed if the image cannot be shown.
        /// Always provide descriptive alternate text for better accessibility.
        /// </summary>
        /// <value>The image alternate text. Default is "image".</value>
        [Parameter]
        public string AlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets the callback invoked when the image is clicked.
        /// Use this to make images interactive, such as opening modal viewers, navigating, or triggering actions.
        /// </summary>
        /// <value>The click event callback.</value>
        [Parameter]
        public EventCallback<MouseEventArgs> Click { get; set; }

        /// <summary>
        /// Handles the <see cref="E:Click" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            await Click.InvokeAsync(args);
        }

        string GetAlternateText()
        {
            if (Attributes != null && Attributes.TryGetValue("alt", out var @alt) && !string.IsNullOrEmpty(Convert.ToString(@alt)))
            {
                return $"{AlternateText} {@alt}";
            }

            return AlternateText;
        }
    }
}

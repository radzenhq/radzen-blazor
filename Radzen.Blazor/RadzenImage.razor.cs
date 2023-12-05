using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenImage component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenImage Path="someimage.png" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenImage : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [Parameter]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string AlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
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

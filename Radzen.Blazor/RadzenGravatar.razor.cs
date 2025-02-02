using Microsoft.AspNetCore.Components;
using System;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenGravatar component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenGravatar Email="info@radzen.com" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenGravatar : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>The email.</value>
        [Parameter]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string AlternateText { get; set; } = "gravatar";

        /// <summary>
        /// Gets or sets the size. Defaulted to 36 (pixels). 
        /// </summary> 
        /// <value>The size of the image in pixels.</value>
        [Parameter]
        public int Size { get; set; } = 36;

        /// <summary>
        /// Gets gravatar URL.
        /// </summary>
        protected string Url
        {
            get
            {
                var md5Email = MD5.Calculate(System.Text.Encoding.ASCII.GetBytes(Email != null ? Email : ""));

                var style = "retro";

                return $"https://secure.gravatar.com/avatar/{md5Email}?d={style}&s={Size}";
            }
        }

        string GetAlternateText()
        {
            if (Attributes != null && Attributes.TryGetValue("alt", out var @alt) && !string.IsNullOrEmpty(Convert.ToString(@alt)))
            {
                return $"{AlternateText} {@alt}";
            }

            return AlternateText;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-gravatar";
        }
    }
}

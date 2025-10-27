using Microsoft.AspNetCore.Components;
using System;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A Gravatar avatar component that displays a user's profile image from Gravatar.com based on their email address.
    /// RadzenGravatar automatically fetches and displays the globally recognized avatar associated with an email.
    /// Gravatar (Globally Recognized Avatar) is a service that associates profile images with email addresses.
    /// Fetches avatar using MD5 hash of email address, requires no storage or management of avatar images, shows default retro-style avatar if email has no Gravatar,
    /// features configurable pixel dimensions, and uses secure.gravatar.com to retrieve images.
    /// Generates a Gravatar URL from the email and displays it as an image. If the email doesn't have a Gravatar account, a retro-style default avatar is shown.
    /// Commonly used in user profiles, comment sections, or anywhere user identity is displayed.
    /// </summary>
    /// <example>
    /// Basic Gravatar:
    /// <code>
    /// &lt;RadzenGravatar Email="user@example.com" /&gt;
    /// </code>
    /// Large Gravatar with custom alternate text:
    /// <code>
    /// &lt;RadzenGravatar Email=@currentUser.Email Size="80" AlternateText=@currentUser.Name /&gt;
    /// </code>
    /// Gravatar in profile header:
    /// <code>
    /// &lt;RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" AlignItems="AlignItems.Center"&gt;
    ///     &lt;RadzenGravatar Email=@user.Email Size="64" /&gt;
    ///     &lt;RadzenText TextStyle="TextStyle.H5"&gt;@user.Name&lt;/RadzenText&gt;
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// </example>
    public partial class RadzenGravatar : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the email address used to fetch the Gravatar image.
        /// The email is hashed (MD5) and used to query Gravatar.com for the associated profile image.
        /// </summary>
        /// <value>The email address.</value>
        [Parameter]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the alternate text describing the avatar for accessibility.
        /// This text is read by screen readers and displayed if the image fails to load.
        /// </summary>
        /// <value>The image alternate text. Default is "gravatar".</value>
        [Parameter]
        public string AlternateText { get; set; } = "gravatar";

        /// <summary>
        /// Gets or sets the size of the avatar image in pixels (both width and height).
        /// Gravatar provides square images at various sizes.
        /// </summary>
        /// <value>The avatar size in pixels. Default is 36.</value>
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

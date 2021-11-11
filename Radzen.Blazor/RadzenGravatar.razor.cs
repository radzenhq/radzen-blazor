using Microsoft.AspNetCore.Components;
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
        /// Gets gravatar URL.
        /// </summary>
        protected string Url
        {
            get
            {
                byte[] hash = new byte[0];

                try
                {
                    hash = System.Security.Cryptography.MD5.Create()
                        .ComputeHash(System.Text.Encoding.ASCII.GetBytes(Email != null ? Email : ""));
                }
                catch
                {
                    //
                }

                var md5Email = string.Join("", Enumerable.Range(0, hash.Length).Select(i => hash[i].ToString("x2")));

                var style = "retro";
                var width = "36";

                return $"https://secure.gravatar.com/avatar/{md5Email}?d={style}&s={width}";
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-gravatar";
        }
    }
}

using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenIcon component. Displays icon from Material Icons font.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenIcon Icon="3d_rotation" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenIcon : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rzi d-inline-flex justify-content-center align-items-center";
        }
    }
}

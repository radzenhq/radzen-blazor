using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProfileMenu component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    /// <example>
    /// <code>
    /// &lt;RadzenProfileMenu&gt;
    ///     &lt;RadzenProfileMenuItem Text="Data"&gt;
    ///         &lt;RadzenProfileMenuItem Text="Orders" Path="orders" /&gt;
    ///         &lt;RadzenProfileMenuItem Text="Employees" Path="employees" /&gt;
    ///     &lt;/RadzenProfileMenuItemItem&gt;
    /// &lt;/RadzenProfileMenu&gt;
    /// </code>
    /// </example>
    public partial class RadzenProfileMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-menu rz-profile-menu";
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<RadzenProfileMenuItem> Click { get; set; }

        string contentStyle = "display:none;position:absolute;z-index:1;";
        string iconStyle = "transform: rotate(0deg);";

        /// <summary>
        /// Toggles the menu open/close state.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public void Toggle(MouseEventArgs args)
        {
            contentStyle = contentStyle.IndexOf("display:none;") != -1 ? "display:block;" : "display:none;position:absolute;z-index:1;";
            iconStyle = iconStyle.IndexOf("rotate(0deg)") != -1 ? "transform: rotate(-180deg);" : "transform: rotate(0deg);";
            StateHasChanged();
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            contentStyle = "display:none;";
            iconStyle = "transform: rotate(0deg);";
            StateHasChanged();
        }
    }
}
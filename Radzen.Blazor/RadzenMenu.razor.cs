using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenMenu component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    /// <example>
    /// <code>
    /// &lt;RadzenMenu&gt;
    ///     &lt;RadzenMenuItem Text="Data"&gt;
    ///         &lt;RadzenMenuItem Text="Orders" Path="orders" /&gt;
    ///         &lt;RadzenMenuItem Text="Employees" Path="employees" /&gt;
    ///     &lt;/RadzenMenuItemItem&gt;
    /// &lt;/RadzenMenu&gt;
    /// </code>
    /// </example>
    public partial class RadzenMenu : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenMenu"/> is responsive.
        /// </summary>
        /// <value><c>true</c> if responsive; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Responsive { get; set; } = true;

        private bool IsOpen { get; set; } = false;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>();

            classList.Add("rz-menu");

            if (Responsive)
            {
                if (IsOpen)
                {
                    classList.Add("rz-menu-open");
                }
                else
                {
                    classList.Add("rz-menu-closed");
                }
            }

            return string.Join(" ", classList);
        }

        void OnToggle()
        {
            IsOpen = !IsOpen;
        }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }
    }
}
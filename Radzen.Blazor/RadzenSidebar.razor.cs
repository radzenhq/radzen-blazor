using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSidebar component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    public partial class RadzenSidebar : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public override string Style { get; set; } = "top:51px;bottom:57px;width:250px;";

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-sidebar";
        }

        /// <summary>
        /// Toggles this instance expanded state.
        /// </summary>
        public void Toggle()
        {
            Expanded = !Expanded;

            StateHasChanged();
        }

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetStyle()
        {
            return $"{Style}{(Expanded ? ";transform:translateX(0px);" : ";width:0px;transform:translateX(-100%);")}";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSidebar"/> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Expanded { get; set; } = true;

        /// <summary>
        /// Gets or sets the expanded changed callback.
        /// </summary>
        /// <value>The expanded changed callback.</value>
        [Parameter]
        public EventCallback<bool> ExpandedChanged { get; set; }
    }
}
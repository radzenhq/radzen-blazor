using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSidebar component.
    /// </summary>
    public partial class RadzenSidebar : RadzenComponentWithChildren
    {
        private const string DefaultStyle = "top:51px;bottom:57px;width:250px;";

        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public override string Style { get; set; } = DefaultStyle;

        /// <summary>
        /// The <see cref="RadzenLayout" /> this component is nested in.
        /// </summary>
        [CascadingParameter]
        public RadzenLayout Layout { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return Expanded ? "rz-sidebar rz-sidebar-expanded" : "rz-sidebar rz-sidebar-collapsed";
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
            var style = Style;

            if (Layout != null && !string.IsNullOrEmpty(style))
            {
                style = style.Replace(DefaultStyle, "");
            }

            if (Layout != null)
            {
                return style;
            }

            return $"{style}{(Expanded ? ";transform:translateX(0px);" : ";width:0px;transform:translateX(-100%);")}";
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

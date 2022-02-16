using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

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
        /// Toggles the responsive mode of the component. If set to <c>true</c> (the default) the component will be 
        /// expanded on larger displays and collapsed on touch devices. Set to <c>false</c> if you want to disable this behavior.
        /// Responsive mode is only available when RadzenSidebar is inside <see cref="RadzenLayout" />.
        /// </summary>
        [Parameter]
        public bool Responsive { get; set; } = true;

        private bool IsResponsive => Responsive && Layout != null;

        /// <summary>
        /// The <see cref="RadzenLayout" /> this component is nested in.
        /// </summary>
        [CascadingParameter]
        public RadzenLayout Layout { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-sidebar").Add("rz-sidebar-expanded", expanded == true)
                                                 .Add("rz-sidebar-collapsed", expanded == false)
                                                 .Add("rz-sidebar-responsive", IsResponsive)
                                                 .ToString();
        }

        /// <summary>
        /// Toggles this instance expanded state.
        /// </summary>
        public void Toggle()
        {
            expanded = Expanded = !Expanded;

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

        bool? expanded;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            if (!Responsive)
            {
                expanded = Expanded;
            }

            base.OnInitialized();
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Expanded), Expanded))
            {
                expanded = parameters.GetValueOrDefault<bool>(nameof(Expanded));
            }

            await base.SetParametersAsync(parameters);
        }

        async Task OnChange(bool matches)
        {
            expanded = !matches;
            await ExpandedChanged.InvokeAsync(!matches);
        }
    }
}

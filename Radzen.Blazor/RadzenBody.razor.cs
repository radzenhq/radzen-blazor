using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenBody component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    public partial class RadzenBody : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public override string Style { get; set; } = "margin-top: 51px; margin-bottom: 57px; margin-left:250px;";

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "body";
        }

        /// <summary>
        /// Toggles this instance width and left margin.
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
            var marginLeft = 250;

            if (!string.IsNullOrEmpty(Style))
            {
                var marginLeftStyle = Style.Split(';').Where(i => i.Split(':')[0].Contains("margin-left")).FirstOrDefault();
                if (!string.IsNullOrEmpty(marginLeftStyle) && marginLeftStyle.Contains("px"))
                {
                    marginLeft = int.Parse(marginLeftStyle.Split(':')[1].Trim().Replace("px", "").Split('.')[0].Trim());
                }
            }

            return $"{Style}; margin-left: {(Expanded ? 0 : marginLeft)}px";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenBody"/> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Expanded { get; set; } = false;

        /// <summary>
        /// Gets or sets a callback raised when the component is expanded or collapsed.
        /// </summary>
        /// <value>The expanded changed callback.</value>
        [Parameter]
        public EventCallback<bool> ExpandedChanged { get; set; }
    }
}

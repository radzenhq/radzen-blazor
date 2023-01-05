using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLabel component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenLabel Text="CompanyName" Component="CompanyName" /&gt;
    /// &lt;RadzenTextBox Name="CompanyName" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenLabel : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the component name for the label.
        /// </summary>
        /// <value>The component name for the label.</value>
        [Parameter]
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the required marker.
        /// </summary>
        /// <value>The text of the required marker.</value>
        [Parameter]
        public string RequiredMarker { get; set; } = string.Empty;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-label";
        }
    }
}

using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenHeading component.
    /// </summary>
    public partial class RadzenHeading : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public string Size { get; set; } = "H1";

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-heading";
        }
    }
}

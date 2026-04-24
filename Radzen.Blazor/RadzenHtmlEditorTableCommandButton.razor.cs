using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A built-in HTML editor tool for table commands.
    /// </summary>
    public partial class RadzenHtmlEditorTableCommandButton : RadzenHtmlEditorButtonBase
    {
        /// <summary>
        /// Gets or sets the executed table command.
        /// </summary>
        [Parameter]
        public string? TableCommand { get; set; }

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool.
        /// </summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// Specifies the icon of the tool.
        /// </summary>
        [Parameter]
        public string? Icon { get; set; }
    }
}

using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A built-in HTML editor toolbar group for table insertion and table commands.
    /// </summary>
    public partial class RadzenHtmlEditorTableTools : RadzenHtmlEditorButtonBase
    {
        /// <summary>
        /// Gets or sets localizable strings used by the table tools. Falls back to <see cref="RadzenHtmlEditor.TableStrings" />.
        /// </summary>
        [Parameter]
        public HtmlEditorTableStrings? TableStrings { get; set; }
    }
}

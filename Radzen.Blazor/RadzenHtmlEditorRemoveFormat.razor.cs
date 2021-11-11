using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which removes the styling of the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorRemoveFormat /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorRemoveFormat : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "removeFormat";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Remove styling"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Remove styling";
    }
}

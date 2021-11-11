using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which justifies the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorJustify /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorJustify : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "justifyFull";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Justify"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Justify";
    }
}

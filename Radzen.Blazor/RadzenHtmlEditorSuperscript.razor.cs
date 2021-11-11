using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which formats the selection as superscript.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorSuperscript /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorSuperscript : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "superscript";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Superscript"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Superscript";
    }
}

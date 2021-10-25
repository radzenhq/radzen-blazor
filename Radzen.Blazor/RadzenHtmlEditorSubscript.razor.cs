using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which formats the selection as subscript.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorSubscript /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorSubscript : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "subscript";


        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Subscript"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Subscript";
    }
}

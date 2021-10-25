using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which applies "strike through" styling to the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorStrikeThrough /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorStrikeThrough : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "strikeThrough";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Strikethrough"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Strikethrough";
    }
}

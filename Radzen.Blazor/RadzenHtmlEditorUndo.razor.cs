using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which reverts the last edit operation.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorUndo /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorUndo : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "undo";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Undo"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Undo";
    }
}

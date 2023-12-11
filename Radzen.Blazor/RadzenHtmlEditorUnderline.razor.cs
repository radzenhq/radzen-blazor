using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which underlines the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorUnderline /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorUnderline : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "underline";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Underline"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Underline";

        /// <summary>
        /// Specifies the shortcut for the command. Set to <c>"Ctrl+U"</c> by default.
        /// </summary>
        [Parameter]
        public override string Shortcut { get; set; } = "Ctrl+U";
    }
}

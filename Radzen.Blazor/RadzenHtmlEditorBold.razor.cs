using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which bolds the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorBold /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorBold : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "bold";

        private string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Bold"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorBold_Title)); set => title = value; }

        /// <summary>
        /// Specifies the shortcut for the command. Set to <c>"Ctrl+B"</c> by default.
        /// </summary>
        [Parameter]
        public override string? Shortcut { get; set; } = "Ctrl+B";
    }
}

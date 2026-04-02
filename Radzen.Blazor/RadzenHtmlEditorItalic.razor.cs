using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which makes the selection italic.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorItalic /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorItalic : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "italic";

        private string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Italic"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorItalic_Title)); set => title = value; }

        /// <summary>
        /// Specifies the shortcut for the command. Set to <c>"Ctrl+I"</c> by default.
        /// </summary>
        [Parameter]
        public override string? Shortcut { get; set; } = "Ctrl+I";
    }
}

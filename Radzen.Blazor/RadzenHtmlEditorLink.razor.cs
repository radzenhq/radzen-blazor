using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which creates links from the selection of a <see cref="RadzenHtmlEditor" />.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorLink /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorLink : RadzenHtmlEditorButtonBase
    {
        class LinkAttributes
        {
            public string? InnerText { get; set; }
            public string? InnerHtml { get; set; }
            public string? Href { get; set; }
            public string? Target { get; set; }
        }

        private string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Insert link"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorLink_Title)); set => title = value; }

        private string? urlText;

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a web address. Set to <c>"Web address"</c> by default.
        /// </summary>
        [Parameter]
        public string UrlText { get => urlText ?? Localize(nameof(RadzenStrings.HtmlEditorLink_UrlText)); set => urlText = value; }

        private string? openInNewWindowText;

        /// <summary>
        /// Specifies the text of the checkbox that opens the link in new window. Set to <c>"Open in new window"</c> by default.
        /// </summary>
        [Parameter]
        public string OpenInNewWindowText { get => openInNewWindowText ?? Localize(nameof(RadzenStrings.HtmlEditorLink_OpenInNewWindowText)); set => openInNewWindowText = value; }

        private string? linkText;

        /// <summary>
        /// Specifies the text of the label suggesting the user to change the text of the link. Set to <c>"Text"</c> by default.
        /// </summary>
        [Parameter]
        public string LinkText { get => linkText ?? Localize(nameof(RadzenStrings.HtmlEditorLink_LinkText)); set => linkText = value; }

        private string? okText;

        /// <summary>
        /// Specifies the text of button which inserts the image. Set to <c>"OK"</c> by default.
        /// </summary>
        [Parameter]
        public string OkText { get => okText ?? Localize(nameof(RadzenStrings.HtmlEditorLink_OkText)); set => okText = value; }

        private string? cancelText;

        /// <summary>
        /// Specifies the text of button which cancels image insertion and closes the dialog. Set to <c>"Cancel"</c> by default.
        /// </summary>
        [Parameter]
        public string CancelText { get => cancelText ?? Localize(nameof(RadzenStrings.HtmlEditorLink_CancelText)); set => cancelText = value; }

        /// <summary>
        /// Specifies the shortcut for the command. Set to <c>"Ctrl+K"</c> by default.
        /// </summary>
        [Parameter]
        public override string? Shortcut { get; set; } = "Ctrl+K";
    }
}

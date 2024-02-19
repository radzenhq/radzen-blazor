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
            public string InnerText { get; set; }
            public string InnerHtml { get; set; }
            public string Href { get; set; }
            public string Target { get; set; }
        }

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Insert link"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Insert link";

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a web address. Set to <c>"Web address"</c> by default.
        /// </summary>
        [Parameter]
        public string UrlText { get; set; } = "Web address";

        /// <summary>
        /// Specifies the text of the checkbox that opens the link in new window. Set to <c>"Open in new window"</c> by default.
        /// </summary>
        [Parameter]
        public string OpenInNewWindowText { get; set; } = "Open in new window";

        /// <summary>
        /// Specifies the text of the label suggesting the user to change the text of the link. Set to <c>"Text"</c> by default.
        /// </summary>
        [Parameter]
        public string LinkText { get; set; } = "Text";

        /// <summary>
        /// Specifies the text of button which inserts the image. Set to <c>"OK"</c> by default.
        /// </summary>
        [Parameter]
        public string OkText { get; set; } = "OK";

        /// <summary>
        /// Specifies the text of button which cancels image insertion and closes the dialog. Set to <c>"Cancel"</c> by default.
        /// </summary>
        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        /// <summary>
        /// Specifies the shortcut for the command. Set to <c>"Ctrl+K"</c> by default.
        /// </summary>
        [Parameter]
        public override string Shortcut { get; set; } = "Ctrl+K";
    }
}

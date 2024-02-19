using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which changes the style of a the selected text by making it a heading or paragraph.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorFormatBlock /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorFormatBlock : ComponentBase, IDisposable
    {
        /// <summary>
        /// The RadzenHtmlEditor component which this tool is part of.
        /// </summary>
        [CascadingParameter]
        public RadzenHtmlEditor Editor { get; set; }

        /// <summary>
        /// Specifies the placeholder displayed to the user. Set to <c>"Format block"</c> by default.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Format block";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Text style"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Text style";

        /// <summary>
        /// Specifies the text displayed for the normal text example. Set to <c>"Normal"</c> by default.
        /// </summary>
        [Parameter]
        public string NormalText { get; set; } = "Normal";

        /// <summary>
        /// Specifies the text displayed for the h1 example. Set to <c>"Heading 1"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading1Text { get; set; } = "Heading 1";

        /// <summary>
        /// Specifies the text displayed for the h2 example. Set to <c>"Heading 2"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading2Text { get; set; } = "Heading 2";

        /// <summary>
        /// Specifies the text displayed for the h3 example. Set to <c>"Heading 3"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading3Text { get; set; } = "Heading 3";

        /// <summary>
        /// Specifies the text displayed for the h4 example. Set to <c>"Heading 4"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading4Text { get; set; } = "Heading 4";

        /// <summary>
        /// Specifies the text displayed for the h5 example. Set to <c>"Heading 5"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading5Text { get; set; } = "Heading 5";

        /// <summary>
        /// Specifies the text displayed for the h6 example. Set to <c>"Heading 6"</c> by default.
        /// </summary>
        [Parameter]
        public string Heading6Text { get; set; } = "Heading 6";

        async Task OnChange(string value)
        {
            await Editor.ExecuteCommandAsync("formatBlock", value);
        }

        /// <inheritdoc/>
        override protected void OnInitialized()
        {
            Editor?.RegisterShortcut("Alt+Shift+1", () => OnChange("h1"));
            Editor?.RegisterShortcut("Alt+Shift+2", () => OnChange("h2"));
            Editor?.RegisterShortcut("Alt+Shift+3", () => OnChange("h3"));
            Editor?.RegisterShortcut("Alt+Shift+4", () => OnChange("h4"));
            Editor?.RegisterShortcut("Alt+Shift+5", () => OnChange("h5"));
            Editor?.RegisterShortcut("Alt+Shift+6", () => OnChange("h6"));
            Editor?.RegisterShortcut("Alt+Shift+7", () => OnChange("p"));
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            Editor?.UnregisterShortcut("Alt+Shift+1");
            Editor?.UnregisterShortcut("Alt+Shift+2");
            Editor?.UnregisterShortcut("Alt+Shift+3");
            Editor?.UnregisterShortcut("Alt+Shift+4");
            Editor?.UnregisterShortcut("Alt+Shift+5");
            Editor?.UnregisterShortcut("Alt+Shift+6");
            Editor?.UnregisterShortcut("Alt+Shift+7");
        }
    }
}

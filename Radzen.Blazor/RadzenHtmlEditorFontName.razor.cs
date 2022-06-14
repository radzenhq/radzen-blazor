using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which changes the font of the selected text.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorFontName /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorFontName
    {
        IList<RadzenHtmlEditorFontNameItem> fonts = new List<RadzenHtmlEditorFontNameItem>();

        internal void AddFont(RadzenHtmlEditorFontNameItem font)
        {
            if (!fonts.Contains(font))
            {
                fonts.Add(font);
            }
        }

        /// <summary>
        /// Sets the child content.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Specifies the placeholder displayed to the user. Set to <c>"Font"</c> by default.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; } = "Font";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Font name"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Font name";

        /// <summary>
        /// The RadzenHtmlEditor component which this tool is part of.
        /// </summary>
        [CascadingParameter]
        public RadzenHtmlEditor Editor { get; set; }

        async Task OnChange(string value)
        {
            await Editor.ExecuteCommandAsync("fontName", value);
        }
    }
}

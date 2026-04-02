using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which changes the font size of the selected text.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorFontSize /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorFontSize
    {
        [Inject]
        private IServiceProvider Services { get; set; } = default!;

        private Localizer? localizer;
        internal Localizer Localizer => localizer ??= Services.GetService<Localizer>() ?? Localizer.Default;
        string Localize(string key) => Localizer.Get(key, CultureInfo.CurrentUICulture);

        /// <summary>
        /// The RadzenHtmlEditor component which this tool is part of.
        /// </summary>
        [CascadingParameter]
        public RadzenHtmlEditor? Editor { get; set; }

        private string? placeholder;

        /// <summary>
        /// Specifies the placeholder displayed to the user. Set to <c>"Font size"</c> by default.
        /// </summary>
        [Parameter]
        public string Placeholder { get => placeholder ?? Localize(nameof(RadzenStrings.HtmlEditorFontSize_Placeholder)); set => placeholder = value; }

        private string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Font size"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorFontSize_Title)); set => title = value; }

        async Task OnChange(string value)
        {
            if (Editor != null)
            {
                await Editor.ExecuteCommandAsync("fontSize", value);
            }
        }

    }
}

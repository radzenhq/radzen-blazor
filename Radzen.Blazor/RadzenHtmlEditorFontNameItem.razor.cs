using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Adds a custom font to a <see cref="RadzenHtmlEditorFontName" />.
    /// </summary>
    /// <example>
    /// <code>
    ///  &lt;RadzenHtmlEditorFontName&gt;
    ///  &lt;RadzenHtmlEditorFontNameItem Text="Times New Roman" Value='"Times New Roman"' /&gt;
    ///  &lt;/RadzenHtmlEditorFontName&gt;
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorFontNameItem
    {
        /// <summary>
        /// The name of the font e.g. <c>"Times New Roman"</c>.
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// The CSS value of the font. Use quotes if it contains spaces.
        /// </summary>
        [Parameter]
        public string Value { get; set; }

        /// <summary>
        /// The RadzenHtmlEditorFontName tool which this tool belongs to.
        /// </summary>
        [CascadingParameter]
        public RadzenHtmlEditorFontName Parent { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            Parent.AddFont(this);
        }
    }
}

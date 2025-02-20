using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which sets the background color of the selection.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorBackground /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorBackground : RadzenHtmlEditorColorBase
    {
        /// <inheritdoc />
        protected override string CommandName => "backColor";

        /// <summary>
        /// Specifies the default background color. Set to <c>"rgb(0, 0, 255)"</c> by default;
        /// </summary>
        [Parameter]
        public override string Value { get; set; } = "rgb(0, 0, 255)";
        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Background color"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Background color";
    }
}

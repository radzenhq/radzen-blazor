using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Adds a custom color to <see cref="RadzenHtmlEditorColor" />.
    /// </summary>
    /// <example>
    /// <code>
    ///  &lt;RadzenHtmlEditorColor &gt;
    ///     &lt;RadzenHtmlEditorColorItem Value="red" /&gt;
    ///     &lt;RadzenHtmlEditorColorItem Value="green" /&gt;
    ///  &lt;/RadzenHtmlEditorColor &gt;
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorColorItem
    {
        /// <summary>
        /// The custom color to add.
        /// </summary>
        [Parameter]
        public string Value { get; set; }
    }
}

using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Adds a custom color to <see cref="RadzenHtmlEditorBackground" />.
    /// </summary>
    /// <example>
    /// <code>
    ///  &lt;RadzenHtmlEditorBackground &gt;
    ///     &lt;RadzenHtmlEditorBackgroundItem Value="red" /&gt;
    ///     &lt;RadzenHtmlEditorBackgroundItem Value="green" /&gt;
    ///  &lt;/RadzenHtmlEditorBackground &gt;
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorBackgroundItem
    {
        /// <summary>
        /// The custom color to add.
        /// </summary>
        [Parameter]
        public string Value { get; set; }
    }
}

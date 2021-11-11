using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which inserts a bullet list (<c>ul</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorUnorderedList /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorUnorderedList : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "insertUnorderedList";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Bullet list"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Bullet list";
    }
}

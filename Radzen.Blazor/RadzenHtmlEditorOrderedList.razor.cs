using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which inserts an ordered list (<c>ol</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorOrderedList /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorOrderedList : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "insertOrderedList";

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Ordered list"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Ordered list";
    }
}

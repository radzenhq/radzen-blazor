using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A RadzenHtmlEditor tool which removes a link.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorUnlink /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!"; 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorUnlink : RadzenHtmlEditorButtonBase
    {
        /// <inheritdoc />
        protected override string CommandName => "unlink";


        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Remove link"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Remove link";
    }
}

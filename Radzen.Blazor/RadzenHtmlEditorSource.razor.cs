using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which switches between rendered and source views in <see cref="RadzenHtmlEditor" />.
    /// </summary>
    public partial class RadzenHtmlEditorSource
    {

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"View source"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "View source";

    }
}

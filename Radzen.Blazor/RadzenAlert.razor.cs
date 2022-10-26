using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenAlert component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAlert AllowCollapse="false""&gt;
    ///     &lt;ChildContent&gt;
    ///         Content
    ///     &lt;/ChildContent&gt;
    /// &lt;/RadzenAlert&gt;
    /// </code>
    /// </example>
    public partial class RadzenAlert : RadzenComponentWithChildren
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-alert";
        }

        /// <summary>
        /// Gets or sets a value indicating whether close is allowed. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if close is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClose { get; set; } = true;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title { get; set; }

        bool? visible;
        bool GetVisible()
        {
            return visible ?? Visible;
        }
    }
}

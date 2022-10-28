using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenAlert component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenAlert&gt;
    ///     &lt;ChildContent&gt;
    ///         Content
    ///     &lt;/ChildContent&gt;
    /// &lt;/RadzenAlert&gt;
    /// </code>
    /// </example>
    public partial class RadzenAlert : RadzenComponentWithChildren
    {
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

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        /// <value>The severity.</value>
        [Parameter]
        public AlertStyle AlertStyle { get; set; } = AlertStyle.Base;

        /// <summary>
        /// Gets or sets the design variant of the alert.
        /// </summary>
        /// <value>The variant of the alert.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the color shade of the alert.
        /// </summary>
        /// <value>The color shade of the alert.</value>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        bool? visible;
        bool GetVisible()
        {
            return visible ?? Visible;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-alert rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(AlertStyle), AlertStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()}";
        }
    }
}

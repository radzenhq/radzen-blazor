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
        private string GetAlertSize()
        {
            return Size == AlertSize.Medium ? "md" : Size == AlertSize.Large ? "lg" : Size == AlertSize.Small ? "sm" : "xs";
        }

        /// <summary>
        /// Gets or sets a value indicating whether close is allowed. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if close is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClose { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether icon should be shown. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if icon is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowIcon { get; set; } = true;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the text of the alert. Overriden by <see cref="RadzenComponentWithChildren.ChildContent" />.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

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

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public AlertSize Size { get; set; } = AlertSize.Medium;

        ButtonSize GetCloseButtonSize()
        {
            return Size == AlertSize.ExtraSmall ? ButtonSize.ExtraSmall : ButtonSize.Small;
        }

        bool? visible;
        bool GetVisible()
        {
            return visible ?? Visible;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-alert rz-alert-{GetAlertSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(AlertStyle), AlertStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()}";
        }

        string getIcon()
        {
            if (!string.IsNullOrEmpty(Icon))
            {
                return Icon;
            }
            else if (AlertStyle == AlertStyle.Primary)
            {
                return "lightbulb_outline";
            }
            else if (AlertStyle == AlertStyle.Secondary)
            {
                return "lightbulb_outline";
            }
            else if (AlertStyle == AlertStyle.Light)
            {
                return "lightbulb_outline";
            }
            else if (AlertStyle == AlertStyle.Base)
            {
                return "lightbulb_outline";
            }
            else if (AlertStyle == AlertStyle.Dark)
            {
                return "lightbulb_outline";
            }
            else if (AlertStyle == AlertStyle.Success)
            {
                return "check_circle_outline";
            }
            else if (AlertStyle == AlertStyle.Danger)
            {
                return "error_outline";
            }
            else if (AlertStyle == AlertStyle.Warning)
            {
                return "warning_amber";
            }
            else if (AlertStyle == AlertStyle.Info)
            {
                return "info_outline";
            }

            return "";
        }

        void Close()
        {
            visible = false;
        }
    }
}

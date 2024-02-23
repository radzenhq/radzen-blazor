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

        Shade GetCloseButtonShade()
        {
            if (Shade == Shade.Light || Shade == Shade.Lighter)
            {
                return Shade.Darker;
            }
            else
            {
                return Shade.Default;
            }
        }

        ButtonStyle GetCloseButtonStyle()
        {
            if (Shade == Shade.Light || Shade == Shade.Lighter)
            {
                switch (AlertStyle)
                {
                    case AlertStyle.Success:
                        return ButtonStyle.Success;
                    case AlertStyle.Danger:
                        return ButtonStyle.Danger;
                    case AlertStyle.Warning:
                        return ButtonStyle.Warning;
                    case AlertStyle.Info:
                        return ButtonStyle.Info;
                    case AlertStyle.Primary:
                        return ButtonStyle.Primary;
                    case AlertStyle.Secondary:
                        return ButtonStyle.Secondary;
                    case AlertStyle.Light:
                    case AlertStyle.Base:
                        return ButtonStyle.Dark;
                    default:
                        return ButtonStyle.Light;
                }
            }
            else
            {
                switch (AlertStyle)
                {
                    case AlertStyle.Light:
                    case AlertStyle.Base:
                        return ButtonStyle.Dark;
                    default:
                        return ButtonStyle.Light;
                }
            }
        }


        bool visible;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            visible = Visible;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-alert rz-alert-{GetAlertSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(AlertStyle), AlertStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()}";
        }

        string GetIcon()
        {
            if (!string.IsNullOrEmpty(Icon))
            {
                return Icon;
            }

            switch (AlertStyle)
            {
                case AlertStyle.Success:
                    return "check_circle";
                case AlertStyle.Danger:
                    return "error";
                case AlertStyle.Warning:
                    return "warning_amber";
                case AlertStyle.Info:
                    return "info";
                default:
                    return "lightbulb";
            }
        }

        async Task OnClose()
        {
            visible = false;

            await VisibleChanged.InvokeAsync(false);
            await Close.InvokeAsync(null);
        }

        /// <summary>
        /// Gets or sets the callback which is invoked when the alert is shown or hidden.
        /// </summary>
        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        /// <summary>
        /// Gets or sets the callback which is invoked when the alert is closed by the user.
        /// </summary>
        [Parameter]
        public EventCallback Close { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged)
            {
                visible = Visible;
            }
        }
    }
}

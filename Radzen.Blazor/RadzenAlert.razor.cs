using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// An alert/notification box component for displaying important messages with semantic colors and optional close functionality.
    /// RadzenAlert provides contextual feedback messages for Success, Info, Warning, Error, and other notification scenarios.
    /// Supports semantic styles (Info, Success, Warning, Danger) for contextual coloring, variants (Filled, Flat, Outlined, Text), 
    /// automatic contextual icons or custom icons via Icon property, optional close button via AllowClose for dismissible alerts,
    /// sizes (ExtraSmall, Small, Medium, Large), and content via Title/Text properties or rich content via ChildContent.
    /// Automatically displays appropriate icons based on AlertStyle (checkmark for Success, warning triangle for Warning, etc.) unless ShowIcon is set to false or a custom Icon is provided.
    /// </summary>
    /// <example>
    /// Basic success alert:
    /// <code>
    /// &lt;RadzenAlert AlertStyle="AlertStyle.Success" Title="Success!" Text="Operation completed successfully." /&gt;
    /// </code>
    /// Warning alert with custom content:
    /// <code>
    /// &lt;RadzenAlert AlertStyle="AlertStyle.Warning" AllowClose="false" ShowIcon="true"&gt;
    ///     &lt;strong&gt;Warning:&lt;/strong&gt; Your session will expire in 5 minutes.
    /// &lt;/RadzenAlert&gt;
    /// </code>
    /// Dismissible info alert:
    /// <code>
    /// @if (showAlert)
    /// {
    ///     &lt;RadzenAlert AlertStyle="AlertStyle.Info" Variant="Variant.Flat" 
    ///                  AllowClose="true" Close=@(() =&gt; showAlert = false)&gt;
    ///         This is an informational message that can be dismissed.
    ///     &lt;/RadzenAlert&gt;
    /// }
    /// </code>
    /// </example>
    public partial class RadzenAlert : RadzenComponentWithChildren
    {
        private string GetAlertSize()
        {
            return Size == AlertSize.Medium ? "md" : Size == AlertSize.Large ? "lg" : Size == AlertSize.Small ? "sm" : "xs";
        }

        /// <summary>
        /// Gets or sets whether the alert can be dismissed by showing a close button.
        /// When enabled, a small X button appears in the top-right corner allowing users to close the alert.
        /// Handle the <see cref="Close"/> event to perform actions when the alert is dismissed.
        /// </summary>
        /// <value><c>true</c> to show the close button; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool AllowClose { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to display the contextual icon based on the <see cref="AlertStyle"/>.
        /// When true, shows an appropriate icon (checkmark for Success, info icon for Info, warning for Warning, etc.).
        /// Set to false to hide the icon, or provide a custom icon via the <see cref="Icon"/> property.
        /// </summary>
        /// <value><c>true</c> to show the contextual icon; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        [Parameter]
        public bool ShowIcon { get; set; } = true;

        /// <summary>
        /// Gets or sets the title text displayed prominently at the top of the alert.
        /// Use this for the main alert heading, with additional details in <see cref="Text"/> or custom content via ChildContent.
        /// </summary>
        /// <value>The alert title.</value>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body text of the alert.
        /// This appears below the title as the main alert message. Overridden by ChildContent if custom content is provided.
        /// </summary>
        /// <value>The alert text content.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a custom Material icon name to display instead of the default contextual icon.
        /// Overrides the automatic icon selection based on <see cref="AlertStyle"/>.
        /// Use Material Symbols icon names (e.g., "info", "warning", "check_circle").
        /// </summary>
        /// <value>The custom Material icon name.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a custom color for the alert icon.
        /// Supports any valid CSS color value. If not set, the icon color matches the alert's semantic style.
        /// </summary>
        /// <value>The icon color as a CSS color value.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the semantic style/severity of the alert.
        /// Determines the color scheme and default icon: Info (blue), Success (green), Warning (orange), Danger (red), etc.
        /// </summary>
        /// <value>The alert style. Default is <see cref="AlertStyle.Base"/>.</value>
        [Parameter]
        public AlertStyle AlertStyle { get; set; } = AlertStyle.Base;

        /// <summary>
        /// Gets or sets the design variant that controls the alert's visual appearance.
        /// Options include Filled (solid background), Flat (subtle background), Outlined (border only), and Text (minimal styling).
        /// </summary>
        /// <value>The alert variant. Default is <see cref="Variant.Filled"/>.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the color intensity shade for the alert.
        /// Works in combination with <see cref="AlertStyle"/> to adjust the color darkness/lightness.
        /// </summary>
        /// <value>The color shade. Default is <see cref="Shade.Default"/>.</value>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the size of the alert component.
        /// Controls the padding, font size, and icon size within the alert.
        /// </summary>
        /// <value>The alert size. Default is <see cref="AlertSize.Medium"/>.</value>
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
        protected override string GetComponentCssClass() => ClassList.Create("rz-alert")
                                                                     .Add($"rz-alert-{GetAlertSize()}")
                                                                     .AddVariant(Variant)
                                                                     .Add($"rz-{Enum.GetName(typeof(AlertStyle), AlertStyle).ToLowerInvariant()}")
                                                                     .AddShade(Shade)
                                                                     .ToString();

        string GetIcon() => !string.IsNullOrEmpty(Icon)
                ? Icon
                : AlertStyle switch
                {
                    AlertStyle.Success => "check_circle",
                    AlertStyle.Danger => "error",
                    AlertStyle.Warning => "warning_amber",
                    AlertStyle.Info => "info",
                    _ => "lightbulb",
                };

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
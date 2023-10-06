using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSplitButton component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenSplitButton Click=@(args => Console.WriteLine($"Value is: {args.Value}"))&gt;
    ///     &lt;ChildContent&gt;
    ///         &lt;RadzenSplitButtonItem Text="Orders" Value="1" /&gt;
    ///         &lt;RadzenSplitButtonItem Text="Employees" Value="2" /&gt;
    ///         &lt;RadzenSplitButtonItem Text="Customers" Value="3" /&gt;
    ///     &lt;/ChildContent&gt;
    /// &lt;/RadzenSelectBar&gt;
    /// </code>
    /// </example>
    public partial class RadzenSplitButton : RadzenComponentWithChildren
    {
        private string getButtonSize()
        {
            return Size == ButtonSize.Medium ? "md" : Size == ButtonSize.Large ? "lg" : Size == ButtonSize.Small ? "sm" : "xs";
        }
        
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; } = "";

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
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the button style.
        /// </summary>
        /// <value>The button style.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        /// <summary>
        /// Gets or sets the design variant of the button.
        /// </summary>
        /// <value>The variant of the button.</value>
        [Parameter]
        public Variant Variant { get; set; } = Variant.Filled;

        /// <summary>
        /// Gets or sets the color shade of the button.
        /// </summary>
        /// <value>The color shade of the button.</value>
        [Parameter]
        public Shade Shade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public ButtonSize Size { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets a value indicating whether this instance busy text is shown.
        /// </summary>
        /// <value><c>true</c> if this instance busy text is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsBusy { get; set; }

        /// <summary>
        /// Gets or sets the busy text.
        /// </summary>
        /// <value>The busy text.</value>
        [Parameter]
        public string BusyText { get; set; } = "";

        /// <summary>
        /// Gets a value indicating whether this instance is disabled.
        /// </summary>
        /// <value><c>true</c> if this instance is disabled; otherwise, <c>false</c>.</value>
        public bool IsDisabled { get => Disabled || IsBusy; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitButton"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the value indication behaviour to always open popup with item on click and not invoke <see cref="Click"/> event.
        /// </summary>
        /// <value><c>true</c> to alway open popup with items; othersie, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool AlwaysOpenPopup { get; set; }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
        public EventCallback<RadzenSplitButtonItem> Click { get; set; }

        /// <summary>
        /// Handles the <see cref="E:Click" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (!Disabled)
            {
                if (AlwaysOpenPopup)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
                    await Click.InvokeAsync(null);
                }
            }
        }

        /// <summary>
        /// Closes this instance popup.
        /// </summary>
        public void Close()
        {
            JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
        }

        /// <summary>
        /// Gets the popup identifier.
        /// </summary>
        /// <value>The popup identifier.</value>
        private string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        private string getButtonCss()
        {
            return $"rz-button rz-button-{getButtonSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(ButtonStyle), ButtonStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()} {(IsDisabled ? " rz-state-disabled" : "")}{(string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon) ? " rz-button-icon-only" : "")}";
        }

        private string getPopupButtonCss()
        {
            return $"rz-splitbutton-menubutton rz-button rz-button-icon-only rz-button-{getButtonSize()} rz-variant-{Enum.GetName(typeof(Variant), Variant).ToLowerInvariant()} rz-{Enum.GetName(typeof(ButtonStyle), ButtonStyle).ToLowerInvariant()} rz-shade-{Enum.GetName(typeof(Shade), Shade).ToLowerInvariant()}{(IsDisabled ? " rz-state-disabled" : "")}";
        }

        private string OpenPopupScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this.parentNode, '{PopupID}')";
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return Disabled ? "rz-splitbutton rz-buttonset rz-state-disabled" : "rz-splitbutton rz-buttonset";
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Close();
            base.Dispose();
        }
    }
}

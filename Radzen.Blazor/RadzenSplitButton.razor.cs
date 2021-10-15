using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSplitButton component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    /// <example>
    /// <code>
    /// &lt;RadzenSplitButton Click=@(args => Console.WriteLine($"Value is: {args.Value}"))&gt;
    ///     &lt;Items&gt;
    ///         &lt;RadzenSplitButtonItem Text="Orders" Value="1" /&gt;
    ///         &lt;RadzenSplitButtonItem Text="Employees" Value="2" /&gt;
    ///         &lt;RadzenSplitButtonItem Text="Customers" Value="3" /&gt;
    ///     &lt;/Items&gt;
    /// &lt;/RadzenSelectBar&gt;
    /// </code>
    /// </example>
    public partial class RadzenSplitButton : RadzenComponentWithChildren
    {
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
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitButton"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

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
                await Click.InvokeAsync(null);
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
            return $"rz-button  rz-button-text-icon-left{(Disabled ? " rz-state-disabled" : "")}";
        }

        private string getPopupButtonCss()
        {
            return $"rz-splitbutton-menubutton rz-button rz-button-icon-only{(Disabled ? " rz-state-disabled" : "")}";
        }

        private string OpenPopupScript()
        {
            if (Disabled)
            {
                return string.Empty;
            }

            return $"Radzen.togglePopup(this.parentNode, '{PopupID}')";
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return Disabled ? "rz-splitbutton rz-buttonset rz-state-disabled" : "rz-splitbutton rz-buttonset";
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// A component that displays its child content in a floating overlay anchored to a target element.
    /// Supports smart positioning, optional width syncing with the target, lazy rendering, and close-on-outside-click behavior.
    /// </summary>
    /// <example>
    /// Toggle a popup relative to a button:
    /// <code>
    /// &lt;RadzenButton Text="Open" Click="@(args =&gt; popup.ToggleAsync(button.Element))" @ref="button" /&gt;
    /// &lt;RadzenPopup @ref="popup" Lazy="true"&gt;
    ///     Popup content
    /// &lt;/RadzenPopup&gt;
    /// @code {
    ///     RadzenButton button;
    ///     RadzenPopup popup;
    /// }
    /// </code>
    /// </example>
    public partial class RadzenPopup : RadzenComponent
    {
#nullable disable

        bool open;
        ElementReference target;

        /// <summary>
        /// Gets a value indicating whether the popup is currently open.
        /// </summary>
        public bool IsOpen => open;

        /// <summary>
        /// Determines whether the popup content is rendered only when open.
        /// </summary>
        [Parameter]
        public bool Lazy { get; set; }

        /// <summary>
        /// Specifies whether to prevent the default action on mouse down.
        /// </summary>
        [Parameter]
        public bool PreventDefault { get; set; }

        /// <summary>
        /// Specifies whether the first element in the popup should be automatically focused.
        /// </summary>
        [Parameter]
        public bool AutoFocusFirstElement { get; set; } = true;

        /// <summary>
        /// Gets or sets the content to be rendered inside the popup.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Event callback triggered when the popup is opened.
        /// </summary>
        [Parameter]
        public EventCallback Open { get; set; }

        /// <summary>
        /// Event callback triggered when the popup is closed.
        /// </summary>
        [Parameter]
        public EventCallback Close { get; set; }

        /// <summary>
        /// Specifies whether the popup should close when clicking outside of it.
        /// </summary>
        [Parameter]
        public bool CloseOnClickOutside { get; set; } = true;

        /// <summary>
        /// Toggles the popup open or closed.
        /// </summary>
        /// <param name="target">The target element reference.</param>
        /// <param name="disableSmartPosition">Whether to disable smart positioning.</param>
        /// <param name="syncWidth">Whether to synchronize the width of the popup with the target element.</param>
        public async Task ToggleAsync(ElementReference target, bool disableSmartPosition = false, bool syncWidth = false)
        {
            open = !open;
            this.target = target;

            if (open)
            {
                await Open.InvokeAsync(null);
                await JSRuntime.InvokeVoidAsync(
                    "Radzen.openPopup",
                    target,
                    GetId(),
                    syncWidth,
                    null,
                    null,
                    null,
                    Reference,
                    nameof(OnClose),
                    CloseOnClickOutside,
                    AutoFocusFirstElement,
                    disableSmartPosition
                );
            }
            else
            {
                await CloseAsync();
            }
        }

        /// <summary>
        /// Closes the popup and sets the target element.
        /// </summary>
        /// <param name="target">The target element reference.</param>
        public async Task CloseAsync(ElementReference target)
        {
            open = false;
            this.target = target;

            await CloseAsync();
        }

        /// <summary>
        /// Invoked from JavaScript to close the popup.
        /// </summary>
        [JSInvokable]
        public async Task OnClose()
        {
            open = false;

            await Close.InvokeAsync(null);
        }

        /// <summary>
        /// Closes the popup.
        /// </summary>
        public async Task CloseAsync()
        {
            await JSRuntime.InvokeVoidAsync("Radzen.closePopup", GetId(), Reference, nameof(OnClose));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable && JSRuntime != null)
            {
                JSRuntime.InvokeVoid("Radzen.destroyPopup", GetId());
            }

            GC.SuppressFinalize(this);
        }
    }
}

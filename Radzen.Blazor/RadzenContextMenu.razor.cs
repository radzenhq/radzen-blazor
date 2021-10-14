using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenContextMenu component.
    /// </summary>
    public partial class RadzenContextMenu
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        public string UniqueID { get; set; }

        /// <summary>
        /// Gets or sets the ContextMenuService.
        /// </summary>
        /// <value>The ContextMenuService.</value>
        [Inject] private ContextMenuService Service { get; set; }

        List<ContextMenu> menus = new List<ContextMenu>();

        /// <summary>
        /// Opens the menu.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="options">The options.</param>
        public async Task Open(MouseEventArgs args, ContextMenuOptions options)
        {
            menus.Clear();
            menus.Add(new ContextMenu() { Options = options, MouseEventArgs = args });

            await InvokeAsync(() => { StateHasChanged(); });
        }

        private bool IsJSRuntimeAvailable { get; set; }

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            IsJSRuntimeAvailable = true;

            var menu = menus.LastOrDefault();
            if (menu != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.openContextMenu",
                    menu.MouseEventArgs.ClientX,
                    menu.MouseEventArgs.ClientY,
                    UniqueID);
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public async Task Close()
        {
            var lastTooltip = menus.LastOrDefault();
            if (lastTooltip != null)
            {
                menus.Remove(lastTooltip);
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", UniqueID);
            }

            await InvokeAsync(() => { StateHasChanged(); });
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", UniqueID);
            }

            Service.OnOpen -= OnOpen;
            Service.OnClose -= OnClose;
            Service.OnNavigate -= OnNavigate;
        }

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            UniqueID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "-").Replace("+", "-").Substring(0, 10);

            Service.OnOpen += OnOpen;
            Service.OnClose += OnClose;
            Service.OnNavigate += OnNavigate;
        }

        void OnOpen(MouseEventArgs args, ContextMenuOptions options)
        {
            Open(args, options).ConfigureAwait(false);
        }

        void OnClose()
        {
            Close().ConfigureAwait(false);
        }

        void OnNavigate()
        {
            JSRuntime.InvokeVoidAsync("Radzen.closePopup", UniqueID);
        }
    }
}
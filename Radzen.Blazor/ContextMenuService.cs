using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Class ContextMenuService.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    public class ContextMenuService : IDisposable
    {
        /// <summary>
        /// Gets or sets the navigation manager.
        /// </summary>
        /// <value>The navigation manager.</value>
        NavigationManager navigationManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuService"/> class.
        /// </summary>
        /// <param name="uriHelper">The URI helper.</param>
        public ContextMenuService(NavigationManager uriHelper)
        {
            navigationManager = uriHelper;

            if (navigationManager != null)
            {
                navigationManager.LocationChanged += UriHelper_OnLocationChanged;
            }
        }

        /// <summary>
        /// Handles the OnLocationChanged event of the UriHelper control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs"/> instance containing the event data.</param>
        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (this.OnNavigate != null)
            {
                this.OnNavigate();
            }
        }

        /// <summary>
        /// Occurs when [on navigate].
        /// </summary>
        public event Action OnNavigate;

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        public event Action OnClose;

        /// <summary>
        /// Occurs when [on open].
        /// </summary>
        public event Action<MouseEventArgs, ContextMenuOptions> OnOpen;

        /// <summary>
        /// Opens the specified arguments.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="items">The items.</param>
        /// <param name="click">The click.</param>
        public void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items, Action<MenuItemEventArgs> click = null)
        {
            var options = new ContextMenuOptions();

            options.Items = items;
            options.Click = click;

            OpenTooltip(args, options);
        }

        /// <summary>
        /// Opens the specified arguments.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="childContent">Content of the child.</param>
        public void Open(MouseEventArgs args, RenderFragment<ContextMenuService> childContent)
        {
            var options = new ContextMenuOptions();

            options.ChildContent = childContent;

            OpenTooltip(args, options);
        }

        /// <summary>
        /// Opens the tooltip.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="options">The options.</param>
        private void OpenTooltip(MouseEventArgs args, ContextMenuOptions options)
        {
            OnOpen?.Invoke(args, options);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            OnClose?.Invoke();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            navigationManager.LocationChanged -= UriHelper_OnLocationChanged;
        }
    }

    /// <summary>
    /// Class ContextMenuOptions.
    /// </summary>
    public class ContextMenuOptions
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<ContextMenuService> ChildContent { get; set; }
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public IEnumerable<ContextMenuItem> Items { get; set; }

        /// <summary>
        /// Gets or sets the click.
        /// </summary>
        /// <value>The click.</value>
        public Action<MenuItemEventArgs> Click { get; set; }
    }

    /// <summary>
    /// Class ContextMenu.
    /// </summary>
    public class ContextMenu
    {
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public ContextMenuOptions Options { get; set; }
        /// <summary>
        /// Gets or sets the mouse event arguments.
        /// </summary>
        /// <value>The mouse event arguments.</value>
        public MouseEventArgs MouseEventArgs { get; set; }
    }

    /// <summary>
    /// Class ContextMenuItem.
    /// </summary>
    public class ContextMenuItem
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
    }
}

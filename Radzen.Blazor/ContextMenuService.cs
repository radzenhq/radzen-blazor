using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    public class ContextMenuService : IDisposable
    {
        NavigationManager navigationManager { get; set; }

        public ContextMenuService(NavigationManager uriHelper)
        {
            navigationManager = uriHelper;

            if (navigationManager != null)
            {
                navigationManager.LocationChanged += UriHelper_OnLocationChanged;
            }
        }

        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (this.OnNavigate != null)
            {
                this.OnNavigate();
            }
        }

        public event Action OnNavigate;

        public event Action OnClose;

        public event Action<MouseEventArgs, ContextMenuOptions> OnOpen;

        public void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items, Action<MenuItemEventArgs> click = null)
        {
            var options = new ContextMenuOptions();

            options.Items = items;
            options.Click = click;

            OpenTooltip(args, options);
        }

        public void Open(MouseEventArgs args, RenderFragment<ContextMenuService> childContent)
        {
            var options = new ContextMenuOptions();

            options.ChildContent = childContent;

            OpenTooltip(args, options);
        }

        private void OpenTooltip(MouseEventArgs args, ContextMenuOptions options)
        {
            OnOpen?.Invoke(args, options);
        }

        public void Close()
        {
            OnClose?.Invoke();
        }

        public void Dispose()
        {
            navigationManager.LocationChanged -= UriHelper_OnLocationChanged;
        }
    }

    public class ContextMenuOptions
    {
        public RenderFragment<ContextMenuService> ChildContent { get; set; }
        public IEnumerable<ContextMenuItem> Items { get; set; }

        public Action<MenuItemEventArgs> Click { get; set; }
    }

    public class ContextMenu
    {
        public ContextMenuOptions Options { get; set; }
        public MouseEventArgs MouseEventArgs { get; set; }
    }

    public class ContextMenuItem
    { 
        public string Text { get; set; }
        public object Value { get; set; }
    }
}

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    public class TooltipService : IDisposable
    {
        NavigationManager navigationManager { get; set; }

        public TooltipService(NavigationManager uriHelper)
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

        public event Action<ElementReference, Type, TooltipOptions> OnOpen;

        public void Open(ElementReference element, RenderFragment<TooltipService> childContent, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.ChildContent = childContent;

            OpenTooltip<object>(element, options);
        }

        public void Open(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;

            OpenTooltip<object>(element, options);
        }

        private void OpenTooltip<T>(ElementReference element, TooltipOptions options)
        {
            OnOpen?.Invoke(element, typeof(T), options);
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

    public enum TooltipPosition
    {
        Left,
        Top,
        Right,
        Bottom
    }

    public class TooltipOptions
    {
        public TooltipPosition Position { get; set; } = TooltipPosition.Bottom;
        public int? Duration { get; set; } = 2000;
        public string Style { get; set; }
        public string CssClass { get; set; }
        public string Text { get; set; }
        public RenderFragment<TooltipService> ChildContent { get; set; }
    }

    public class Tooltip
    {
        public Type Type { get; set; }
        public TooltipOptions Options { get; set; }
        public ElementReference Element { get; set; }
    }
}

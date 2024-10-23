using Microsoft.AspNetCore.Components;
using System;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Class ChartTooltipService. Contains methods with options to open and close chart tooltips. 
    /// Should be added as scoped service in the application services and RadzenChartTooltip should be added in application main layout.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    public class ChartTooltipService : IDisposable
    {
        /// <summary>
        /// Gets or sets the navigation manager.
        /// </summary>
        /// <value>The navigation manager.</value>
        NavigationManager navigationManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipService"/> class.
        /// </summary>
        /// <param name="uriHelper">The URI helper.</param>
        public ChartTooltipService(NavigationManager uriHelper)
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
        public event Action<ElementReference, double, double, ChartTooltipOptions> OnOpen;

        /// <summary>
        /// Opens the specified element.
        /// </summary>
        /// <param name="element">The chart element.</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="childContent">Content of the chart tooltip.</param>
        /// <param name="o">The options of the chart tooltip.</param>
        public void Open(ElementReference element, double x, double y, RenderFragment<ChartTooltipService> childContent, ChartTooltipOptions o = null)
        {
            var options = o ?? new ChartTooltipOptions();

            options.ChildContent = childContent;

            OnOpen?.Invoke(element, x, y, options);
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
    /// Class ChartTooltipOptions.
    /// </summary>
    public class ChartTooltipOptions
    {
        /// <summary>
        /// Gets or sets the color scheme used to render the tooltip.
        /// </summary>
        /// <value>The color scheme.</value>
        public ColorScheme ColorScheme { get; set; }
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<ChartTooltipService> ChildContent { get; set; }
    }
}

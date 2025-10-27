using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// A service for displaying tooltips programmatically on UI elements or at specific positions.
    /// TooltipService provides methods to show tooltips with text or custom HTML content, with configurable positioning, delays, and durations.
    /// To use this service, add it as a scoped service in your application's service collection and place a RadzenTooltip component in your main layout.
    /// Manages tooltip lifecycle, automatically closing tooltips on navigation and providing various positioning options (top, bottom, left, right).
    /// Tooltips can be shown on mouse enter/leave events or on demand, with configurable delays before showing and auto-close durations.
    /// </summary>
    /// <example>
    /// Show a simple text tooltip:
    /// <code>
    /// @inject TooltipService TooltipService
    /// &lt;RadzenButton Text="Hover me" MouseEnter="@(args =&gt; TooltipService.Open(args, "This is a tooltip"))" /&gt;
    /// </code>
    /// Show a tooltip with HTML content and custom options:
    /// <code>
    /// @inject TooltipService TooltipService
    /// &lt;RadzenButton Text="Show tooltip" MouseEnter="@(args =&gt; ShowTooltipWithHtml(args))" /&gt;
    /// @code {
    ///     void ShowTooltipWithHtml(ElementReference element) =&gt; TooltipService.Open(element, ts =&gt;
    ///         @&lt;div&gt;
    ///             &lt;b&gt;Bold&lt;/b&gt; and &lt;i&gt;italic&lt;/i&gt; content
    ///         &lt;/div&gt;, 
    ///         new TooltipOptions { Position = TooltipPosition.Top, Duration = 5000 });
    /// }
    /// </code>
    /// </example>
    public class TooltipService : IDisposable
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
        public TooltipService(NavigationManager uriHelper)
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
        public event Action<ElementReference, Type, TooltipOptions> OnOpen;

        /// <summary>
        /// Occurs when [on open chart tooltip].
        /// </summary>
        internal event Action<ElementReference, double, double, ChartTooltipOptions> OnOpenChartTooltip;

        /// <summary>
        /// Opens a tooltip with custom HTML content near the specified element.
        /// The tooltip will be positioned according to the options and can contain any Blazor markup.
        /// </summary>
        /// <param name="element">The HTML element reference near which the tooltip will be displayed.</param>
        /// <param name="childContent">A render fragment that defines the custom HTML content of the tooltip. Receives the TooltipService as context.</param>
        /// <param name="options">Optional tooltip configuration including position, duration, delay, and styling. If null, default options are used.</param>
        public void Open(ElementReference element, RenderFragment<TooltipService> childContent, TooltipOptions options = null)
        {
            var tooltipOptions = options ?? new TooltipOptions();

            tooltipOptions.ChildContent = childContent;

            OpenTooltip<object>(element, tooltipOptions);
        }

        /// <summary>
        /// Opens a tooltip with simple text content near the specified element.
        /// This is the most common way to show basic informational tooltips.
        /// </summary>
        /// <param name="element">The HTML element reference near which the tooltip will be displayed.</param>
        /// <param name="text">The text content to display in the tooltip.</param>
        /// <param name="options">Optional tooltip configuration including position, duration, delay, and styling. If null, default options are used.</param>
        public void Open(ElementReference element, string text, TooltipOptions options = null)
        {
            var tooltipOptions = options ?? new TooltipOptions();

            tooltipOptions.Text = text;

            OpenTooltip<object>(element, tooltipOptions);
        }

        /// <summary>
        /// Opens a tooltip with text content positioned above the specified element.
        /// This is a convenience method equivalent to calling Open() with TooltipPosition.Top.
        /// </summary>
        /// <param name="element">The HTML element reference above which the tooltip will be displayed.</param>
        /// <param name="text">The text content to display in the tooltip.</param>
        /// <param name="options">Optional additional tooltip configuration. The Position will be set to Top regardless of the value in options.</param>
        public void OpenOnTheTop(ElementReference element, string text, TooltipOptions options = null)
        {
            var tooltipOptions = options ?? new TooltipOptions();

            tooltipOptions.Text = text;
            tooltipOptions.Position = TooltipPosition.Top;

            OpenTooltip<object>(element, tooltipOptions);
        }

        /// <summary>
        /// Opens the specified element on the bottom position.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        public void OpenOnTheBottom(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;
            options.Position = TooltipPosition.Bottom;

            OpenTooltip<object>(element, options);
        }

        /// <summary>
        /// Opens the specified element on the left position.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        public void OpenOnTheLeft(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;
            options.Position = TooltipPosition.Left;

            OpenTooltip<object>(element, options);
        }

        /// <summary>
        /// Opens the specified element on the right position.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        public void OpenOnTheRight(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;
            options.Position = TooltipPosition.Right;

            OpenTooltip<object>(element, options);
        }

        /// <summary>
        /// Opens the specified chart tooltip.
        /// </summary>
        /// <param name="element">The chart element.</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="childContent">Content of the chart tooltip.</param>
        /// <param name="o">The options of the chart tooltip.</param>
        internal void OpenChartTooltip(ElementReference element, double x, double y, RenderFragment<TooltipService> childContent, ChartTooltipOptions o = null)
        {
            var options = o ?? new ChartTooltipOptions();

            options.ChildContent = childContent;

            OnOpenChartTooltip?.Invoke(element, x, y, options);
        }

        /// <summary>
        /// Opens the tooltip.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element">The element.</param>
        /// <param name="options">The options.</param>
        private void OpenTooltip<T>(ElementReference element, TooltipOptions options)
        {
            OnOpen?.Invoke(element, typeof(T), options);
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
    /// Enum TooltipPosition
    /// </summary>
    public enum TooltipPosition
    {
        /// <summary>
        /// The left
        /// </summary>
        Left,
        /// <summary>
        /// The top
        /// </summary>
        Top,
        /// <summary>
        /// The right
        /// </summary>
        Right,
        /// <summary>
        /// The bottom
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Class TooltipOptions.
    /// </summary>
    public class TooltipOptions
    {
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        public TooltipPosition Position { get; set; } = TooltipPosition.Bottom;
        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public int? Duration { get; set; } = 2000;
        /// <summary>
        /// Gets or sets the delay.
        /// </summary>
        /// <value>The delay.</value>
        public int? Delay { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the tooltip should be closed by clicking the document.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>.</value>
        public bool CloseTooltipOnDocumentClick { get; set; } = true;
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        public string Style { get; set; }
        /// <summary>
        /// Gets or sets the CSS class.
        /// </summary>
        /// <value>The CSS class.</value>
        public string CssClass { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<TooltipService> ChildContent { get; set; }
    }

    /// <summary>
    /// Class Tooltip.
    /// </summary>
    public class Tooltip
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type { get; set; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public TooltipOptions Options { get; set; }
        /// <summary>
        /// Gets or sets the element.
        /// </summary>
        /// <value>The element.</value>
        public ElementReference Element { get; set; }
    }

    internal class ChartTooltipOptions
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
        public RenderFragment<TooltipService> ChildContent { get; set; }
    }
}

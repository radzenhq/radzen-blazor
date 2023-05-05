using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Class TooltipService. Contains various methods with options to open and close tooltips. 
    /// Should be added as scoped service in the application services and RadzenTooltip should be added in application main layout.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <example>
    /// <code>
    /// @inject TooltipService tooltipService
    /// &lt;RadzenButton Text="Show tooltip" MouseEnter="@(args =&gt; ShowTooltipWithHtml(args, new TooltipOptions(){ Style = "color:#000", Duration = null }))" /&gt;
    /// @code {
    ///     void ShowTooltipWithHtml(ElementReference elementReference, TooltipOptions options = null) =&gt; tooltipService.Open(elementReference, ds =&gt;
    ///         @&lt;div&gt;
    ///             Some&lt;b&gt;HTML&lt;/b&gt; content
    ///         &lt;/div&gt;, options);
    ///     }
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
        /// Opens the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="childContent">Content of the child.</param>
        /// <param name="o">The o.</param>
        public void Open(ElementReference element, RenderFragment<TooltipService> childContent, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.ChildContent = childContent;

            OpenTooltip<object>(element, options);
        }

        /// <summary>
        /// Opens the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        public void Open(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;

            OpenTooltip<object>(element, options);
        }

        /// <summary>
        /// Opens the specified element on the top position.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        public void OpenOnTheTop(ElementReference element, string text, TooltipOptions o = null)
        {
            var options = o ?? new TooltipOptions();

            options.Text = text;
            options.Position = TooltipPosition.Top;

            OpenTooltip<object>(element, options);
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
}

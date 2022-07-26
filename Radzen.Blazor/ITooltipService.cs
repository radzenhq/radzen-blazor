using Microsoft.AspNetCore.Components;
using System;

namespace Radzen
{
    /// <summary>
    /// Tooltip Service Interface
    /// Contains various methods with options to open and close tooltips. 
    /// </summary>
    /// <example>
    /// <code>
    /// @inject ITooltipService tooltipService
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
    public interface ITooltipService
    {
        /// <summary>
        /// Raises the Close event.
        /// </summary>
        event Action OnClose;

        /// <summary>
        /// Occurs when [on navigate].
        /// </summary>
        event Action OnNavigate;

        /// <summary>
        /// Occurs when [on open].
        /// </summary>
        event Action<ElementReference, Type, TooltipOptions> OnOpen;

        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();

        /// <summary>
        /// Opens the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="childContent">Content of the child.</param>
        /// <param name="o">The o.</param>
        void Open(ElementReference element, RenderFragment<TooltipService> childContent, TooltipOptions o = null);

        /// <summary>
        /// Opens the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="text">The text.</param>
        /// <param name="o">The o.</param>
        void Open(ElementReference element, string text, TooltipOptions o = null);
    }
}
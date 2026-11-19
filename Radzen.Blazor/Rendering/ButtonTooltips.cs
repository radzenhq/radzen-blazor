using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Shows the hints of a component's own buttons as Radzen tooltips when a <see cref="TooltipService"/> is registered
    /// and a <see cref="RadzenTooltip"/> displays them, and as native <c>title</c> attributes otherwise.
    /// Its hover callbacks have no receiver, so pointing at a button does not re-render the component that owns it.
    /// </summary>
    internal sealed class ButtonTooltips
    {
        private readonly TooltipService? service;
        private bool native = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonTooltips"/> class.
        /// </summary>
        /// <param name="service">The tooltip service, or <c>null</c> when the application has not registered one.</param>
        public ButtonTooltips(TooltipService? service)
        {
            this.service = service;

            if (service != null)
            {
                Hide = new EventCallback<ElementReference>(null, (Action<ElementReference>)(_ => Close()));
            }
        }

        /// <summary>
        /// Gets the callback that closes the tooltip when the pointer leaves a button. It has no delegate without a tooltip service.
        /// </summary>
        public EventCallback<ElementReference> Hide { get; }

        /// <summary>
        /// Gets the attributes that carry the hint of a button in the current render: its accessible name when the hint
        /// shows as a Radzen tooltip, or its <c>title</c> when it does not.
        /// </summary>
        /// <param name="text">The hint.</param>
        /// <param name="iconOnly">Whether the button shows no text, so the hint is also its accessible name.</param>
        public Dictionary<string, object> Attributes(string text, bool iconOnly = true)
        {
            native = service?.HasHost != true;

            var attributes = new Dictionary<string, object>();

            if (native)
            {
                attributes["title"] = text;
            }
            else if (iconOnly)
            {
                attributes["aria-label"] = text;
            }

            return attributes;
        }

        /// <summary>
        /// Gets the callback that opens the tooltip of a button when the pointer enters it. It has no delegate without a tooltip service.
        /// </summary>
        /// <param name="text">The hint.</param>
        /// <param name="position">Where the tooltip shows relative to the button.</param>
        public EventCallback<ElementReference> Show(string text, TooltipPosition position)
        {
            if (service == null)
            {
                return default;
            }

            return new EventCallback<ElementReference>(null, (Action<ElementReference>)(element =>
            {
                if (!native)
                {
                    service.Open(element, text, new TooltipOptions { Position = position });
                }
            }));
        }

        /// <summary>
        /// Closes the tooltip. A click on a button does not close the tooltip the button shows, so its click handler calls this.
        /// </summary>
        public void Close()
        {
            if (!native)
            {
                service?.Close();
            }
        }
    }
}

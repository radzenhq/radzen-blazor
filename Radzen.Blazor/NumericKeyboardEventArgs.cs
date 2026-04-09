using Microsoft.AspNetCore.Components.Web;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="Radzen.Blazor.RadzenNumeric{TValue}.KeyDown" /> event that is being raised.
    /// Wraps the original <see cref="KeyboardEventArgs" /> and allows the handler to prevent the default
    /// ArrowUp/ArrowDown increment/decrement behavior.
    /// </summary>
    public class NumericKeyboardEventArgs
    {
        /// <summary>
        /// Gets the original <see cref="KeyboardEventArgs" /> raised by the underlying input element.
        /// </summary>
        public KeyboardEventArgs OriginalEvent { get; init; } = new KeyboardEventArgs();

        /// <summary>
        /// Gets a value indicating whether the default action has been prevented.
        /// </summary>
        public bool IsDefaultPrevented { get; private set; }

        /// <summary>
        /// Prevents the default action (ArrowUp/ArrowDown increment/decrement) from occurring.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }
}

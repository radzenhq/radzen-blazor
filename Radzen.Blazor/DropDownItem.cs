using System.Diagnostics.CodeAnalysis;

namespace Radzen
{
    /// <summary>
    /// Represents a key-value pair used in drop-down lists. Provides a trim-safe alternative
    /// to anonymous types for data binding with TextProperty and ValueProperty.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    public class DropDownItem<TValue>
    {
        /// <summary>
        /// Gets or sets the display text.
        /// </summary>
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public TValue Value { get; set; } = default!;
    }
}

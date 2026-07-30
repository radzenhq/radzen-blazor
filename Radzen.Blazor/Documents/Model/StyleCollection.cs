using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents;


/// <summary>
/// The named styles of a document. The built-in <see cref="Normal"/> style always exists and is the
/// default base style for styles added without an explicit base. The built-in <c>Heading1</c> to
/// <c>Heading6</c> styles also always exist and carry the matching <see cref="Style.HeadingLevel"/>.
/// </summary>
public sealed class StyleCollection : IReadOnlyCollection<Style>
{
    private const string NormalName = "Normal";

    private readonly Dictionary<string, Style> styles = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new <see cref="StyleCollection"/> containing only the built-in <see cref="Normal"/> style.
    /// </summary>
    public StyleCollection()
    {
        Normal = new Style(NormalName, null);
        styles.Add(NormalName, Normal);

        for (var level = 1; level <= 6; level++)
        {
            var name = "Heading" + level.ToString(CultureInfo.InvariantCulture);
            styles.Add(name, new Style(name, NormalName) { HeadingLevel = level });
        }
    }

    /// <summary>Gets the built-in <c>Normal</c> style.</summary>
    public Style Normal { get; }

    /// <inheritdoc/>
    public int Count => styles.Count;

    /// <summary>
    /// Gets the style with the specified name.
    /// </summary>
    /// <param name="name">The style name.</param>
    /// <returns>The matching style.</returns>
    /// <exception cref="KeyNotFoundException">No style with <paramref name="name"/> exists.</exception>
    public Style this[string name] => styles[name];

    /// <summary>
    /// Adds a new style that inherits from the specified base style.
    /// </summary>
    /// <param name="name">The unique name of the new style.</param>
    /// <param name="baseStyle">The name of the base style. Defaults to <c>Normal</c>.</param>
    /// <returns>The newly created style.</returns>
    /// <exception cref="ArgumentException">
    /// A style named <paramref name="name"/> already exists, or <paramref name="baseStyle"/> does not exist.
    /// </exception>
    public Style Add(string name, string baseStyle = NormalName)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(baseStyle);

        if (styles.ContainsKey(name))
        {
            throw new ArgumentException($"A style named '{name}' already exists.", nameof(name));
        }

        if (!styles.ContainsKey(baseStyle))
        {
            throw new ArgumentException($"The base style '{baseStyle}' does not exist.", nameof(baseStyle));
        }

        var style = new Style(name, baseStyle);
        styles.Add(name, style);
        return style;
    }

    /// <summary>
    /// Determines whether a style with the specified name exists.
    /// </summary>
    /// <param name="name">The style name.</param>
    /// <returns><see langword="true"/> if the style exists; otherwise <see langword="false"/>.</returns>
    public bool Contains(string name) => styles.ContainsKey(name);

    /// <inheritdoc/>
    public IEnumerator<Style> GetEnumerator() => styles.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

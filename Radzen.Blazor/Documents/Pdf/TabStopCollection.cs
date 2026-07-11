using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// An ordered, read-only view of the explicit tab stops defined on a paragraph.
/// </summary>
public class TabStopCollection : IReadOnlyList<TabStop>
{
    private readonly List<TabStop> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public TabStop this[int index] => items[index];

    /// <summary>
    /// Adds a tab stop at the specified position and alignment.
    /// </summary>
    /// <param name="position">The distance of the stop from the paragraph content-box left edge.</param>
    /// <param name="alignment">The alignment applied to the text following the tab. Defaults to <see cref="TabAlignment.Left"/>.</param>
    /// <returns>The newly created tab stop.</returns>
    public TabStop AddTabStop(Unit position, TabAlignment alignment = TabAlignment.Left)
    {
        var stop = new TabStop(position, alignment);
        items.Add(stop);
        return stop;
    }

    /// <summary>
    /// Appends an existing tab stop.
    /// </summary>
    /// <param name="stop">The tab stop to append.</param>
    /// <returns>The same <paramref name="stop"/> instance.</returns>
    public TabStop Add(TabStop stop)
    {
        items.Add(stop);
        return stop;
    }

    /// <inheritdoc/>
    public IEnumerator<TabStop> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

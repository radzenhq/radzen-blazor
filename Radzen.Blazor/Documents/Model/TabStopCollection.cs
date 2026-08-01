using System;
using System.Collections;
using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the explicit tab stops defined on a paragraph.
/// </summary>
public sealed class TabStopCollection : IReadOnlyList<TabStop>
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
    /// <param name="leader">The character repeated to fill the tab gap (e.g. '.' for dot leaders). '\0' (the default) fills the gap with blank space.</param>
    /// <returns>The newly created tab stop.</returns>
    public TabStop Add(Unit position, TabAlignment alignment = TabAlignment.Left, char leader = '\0')
        => Add(new TabStop(position, alignment, leader));

    /// <summary>
    /// Appends an existing tab stop.
    /// </summary>
    /// <param name="stop">The tab stop to append.</param>
    /// <returns>The same <paramref name="stop"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stop"/> is <see langword="null"/>.</exception>
    public TabStop Add(TabStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        items.Add(stop);
        return stop;
    }

    /// <summary>
    /// Inserts an existing tab stop at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="stop">The tab stop to insert.</param>
    /// <returns>The same <paramref name="stop"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stop"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public TabStop Insert(int index, TabStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        items.Insert(index, stop);
        return stop;
    }

    /// <summary>
    /// Removes the specified tab stop.
    /// </summary>
    /// <param name="stop">The tab stop to remove.</param>
    /// <returns><see langword="true"/> if the stop was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stop"/> is <see langword="null"/>.</exception>
    public bool Remove(TabStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        return items.Remove(stop);
    }

    /// <summary>
    /// Removes the tab stop at the specified position.
    /// </summary>
    /// <param name="index">The zero-based index of the tab stop to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index) => items.RemoveAt(index);

    /// <summary>
    /// Removes every tab stop.
    /// </summary>
    public void Clear() => items.Clear();

    /// <inheritdoc/>
    public IEnumerator<TabStop> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

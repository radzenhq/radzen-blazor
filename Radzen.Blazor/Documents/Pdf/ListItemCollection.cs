using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An ordered, read-only view of the items in a <see cref="List"/> with typed helpers for appending.
/// </summary>
public sealed class ListItemCollection : IReadOnlyList<ListItem>
{
    private readonly System.Collections.Generic.List<ListItem> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public ListItem this[int index] => items[index];

    /// <summary>Appends an empty item.</summary>
    /// <returns>The newly created item.</returns>
    public ListItem Add() => Add(new ListItem());

    /// <summary>Appends an item containing the specified text.</summary>
    /// <param name="text">The item text.</param>
    /// <returns>The newly created item.</returns>
    public ListItem Add(string text)
    {
        var item = new ListItem { Text = text };
        return Add(item);
    }

    /// <summary>Appends an existing item.</summary>
    /// <param name="item">The item to append.</param>
    /// <returns>The same <paramref name="item"/> instance.</returns>
    public ListItem Add(ListItem item)
    {
        System.ArgumentNullException.ThrowIfNull(item);
        items.Add(item);
        return item;
    }

    /// <inheritdoc/>
    public IEnumerator<ListItem> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

using System;
using System.Collections;
using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the items in a <see cref="ListBlock"/> with typed helpers for appending.
/// An item belongs to exactly one list: adding an item that already has a parent throws, as does
/// adding an item to a list nested inside it.
/// </summary>
public sealed class ListItemCollection : IReadOnlyList<ListItem>
{
    internal ListItemCollection()
    {
    }

    private readonly TrackedList<ListItem> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public ListItem this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>Appends an empty item.</summary>
    /// <returns>The newly created item.</returns>
    public ListItem Add() => Add(new ListItem());

    /// <summary>Appends an item containing the specified text.</summary>
    /// <param name="text">The item text.</param>
    /// <returns>The newly created item.</returns>
    public ListItem Add(string text) => Add(new ListItem { Text = text });

    /// <summary>Appends an existing item.</summary>
    /// <param name="item">The item to append.</param>
    /// <returns>The same <paramref name="item"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="item"/> already belongs to another list, or appending it would make the
    /// document tree cyclic.
    /// </exception>
    public ListItem Add(ListItem item)
    {
        ContentTree.Attach(item, this);
        items.Add(item);
        return item;
    }

    /// <summary>Inserts an empty item at the specified position.</summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <returns>The newly created item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public ListItem Insert(int index) => Insert(index, new ListItem());

    /// <summary>Inserts an item containing the specified text at the specified position.</summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="text">The item text.</param>
    /// <returns>The newly created item.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public ListItem Insert(int index, string text) => Insert(index, new ListItem { Text = text });

    /// <summary>Inserts an existing item at the specified position.</summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="item">The item to insert.</param>
    /// <returns>The same <paramref name="item"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="item"/> already belongs to another list, or inserting it would make the
    /// document tree cyclic.
    /// </exception>
    public ListItem Insert(int index, ListItem item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(item, this);
        items.Insert(index, item);
        return item;
    }

    /// <summary>Removes the specified item, detaching it so it may be added elsewhere.</summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><see langword="true"/> if the item was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    public bool Remove(ListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!items.Remove(item))
        {
            return false;
        }

        ContentTree.Detach(item);
        return true;
    }

    /// <summary>Removes the item at the specified position, detaching it so it may be added elsewhere.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var item = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(item);
    }

    /// <summary>
    /// Moves the item at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// items in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the item to move.</param>
    /// <param name="toIndex">The zero-based index the item ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>Removes every item, detaching each one so it may be added elsewhere.</summary>
    public void Clear()
    {
        foreach (var item in items)
        {
            ContentTree.Detach(item);
        }

        items.Clear();
    }

    /// <inheritdoc/>
    public IEnumerator<ListItem> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

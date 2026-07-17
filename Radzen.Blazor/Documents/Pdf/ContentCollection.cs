using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An ordered collection of <see cref="ContentElement"/> instances for a page.
/// Insertion order is the paint (z) order.
/// </summary>
public sealed class ContentCollection : IReadOnlyList<ContentElement>
{
    private readonly List<ContentElement> items = [];
    private bool structureChanged;

    /// <summary>Gets the number of elements in the collection.</summary>
    public int Count => items.Count;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based element index.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
    public ContentElement this[int index] => items[index];

    /// <summary>
    /// Appends an element and returns the same instance for fluent configuration.
    /// </summary>
    /// <typeparam name="T">The concrete element type.</typeparam>
    /// <param name="element">The element to append.</param>
    /// <returns>The same <paramref name="element"/> instance.</returns>
    public T Add<T>(T element)
        where T : ContentElement
    {
        ArgumentNullException.ThrowIfNull(element);
        items.Add(element);
        structureChanged = true;
        return element;
    }

    /// <summary>Inserts an element at the specified paint-order index.</summary>
    /// <typeparam name="T">The concrete element type.</typeparam>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="element">The element to insert.</param>
    /// <returns>The same <paramref name="element"/> instance.</returns>
    public T Insert<T>(int index, T element)
        where T : ContentElement
    {
        ArgumentNullException.ThrowIfNull(element);
        items.Insert(index, element);
        structureChanged = true;
        return element;
    }

    /// <summary>Removes the first occurrence of an element.</summary>
    /// <param name="element">The element to remove.</param>
    /// <returns><see langword="true"/> when the element was removed.</returns>
    public bool Remove(ContentElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var removed = items.Remove(element);
        structureChanged |= removed;
        return removed;
    }

    /// <summary>Removes the element at the specified paint-order index.</summary>
    /// <param name="index">The zero-based element index.</param>
    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        structureChanged = true;
    }

    /// <inheritdoc />
    public IEnumerator<ContentElement> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Clear()
    {
        structureChanged |= items.Count > 0;
        items.Clear();
    }

    // Element flags answer "did an element change", not "did the collection change": a removed
    // element leaves every survivor clean.
    internal bool IsModified
    {
        get
        {
            if (structureChanged)
            {
                return true;
            }

            foreach (var item in items)
            {
                if (item.IsModified)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal void AcceptChanges()
    {
        structureChanged = false;
        foreach (var item in items)
        {
            item.AcceptChanges();
        }
    }
}

using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// An ordered collection of <see cref="ContentElement"/> instances for a page.
/// Insertion order is the paint (z) order.
/// </summary>
public sealed class ContentCollection : IReadOnlyList<ContentElement>
{
    private readonly List<ContentElement> items = [];

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
        System.ArgumentNullException.ThrowIfNull(element);
        items.Add(element);
        return element;
    }

    /// <inheritdoc />
    public IEnumerator<ContentElement> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

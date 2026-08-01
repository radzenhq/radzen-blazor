using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the inline content of a paragraph or list item. An inline belongs to
/// exactly one collection: adding one that already has a parent throws.
/// </summary>
public sealed class InlineCollection : IReadOnlyList<Inline>
{
    private readonly TrackedList<Inline> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Inline this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends a new run with the specified text.
    /// </summary>
    /// <param name="text">The run text.</param>
    /// <returns>The newly created run.</returns>
    public Run Add(string text)
    {
        var run = new Run(text);
        Add(run);
        return run;
    }

    /// <summary>
    /// Appends existing inline content.
    /// </summary>
    /// <param name="inline">The inline to append.</param>
    /// <returns>The same <paramref name="inline"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inline"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="inline"/> already belongs to another collection.</exception>
    public Inline Add(Inline inline)
    {
        ContentTree.Attach(inline, this);
        items.Add(inline);
        return inline;
    }

    /// <summary>
    /// Inserts a new run with the specified text at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="text">The run text.</param>
    /// <returns>The newly created run.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Run Insert(int index, string text)
    {
        var run = new Run(text);
        Insert(index, run);
        return run;
    }

    /// <summary>
    /// Inserts existing inline content at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="inline">The inline to insert.</param>
    /// <returns>The same <paramref name="inline"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inline"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="inline"/> already belongs to another collection.</exception>
    public Inline Insert(int index, Inline inline)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(inline, this);
        items.Insert(index, inline);
        return inline;
    }

    /// <summary>
    /// Appends an inline image that flows within the paragraph line, buffering the bytes from
    /// the specified stream.
    /// </summary>
    /// <param name="stream">The source image stream.</param>
    /// <returns>The newly created inline image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public InlineImage AddImage(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var image = InlineImage.FromStream(stream);
        Add(image);
        return image;
    }

    /// <summary>
    /// Removes the specified inline, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="inline">The inline to remove.</param>
    /// <returns><see langword="true"/> if the inline was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inline"/> is <see langword="null"/>.</exception>
    public bool Remove(Inline inline)
    {
        ArgumentNullException.ThrowIfNull(inline);

        if (!items.Remove(inline))
        {
            return false;
        }

        ContentTree.Detach(inline);
        return true;
    }

    /// <summary>
    /// Removes the inline at the specified position, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="index">The zero-based index of the inline to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var inline = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(inline);
    }

    /// <summary>
    /// Moves the inline at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// inlines in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the inline to move.</param>
    /// <param name="toIndex">The zero-based index the inline ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>
    /// Removes every inline, detaching each one so it may be added elsewhere.
    /// </summary>
    public void Clear()
    {
        foreach (var inline in items)
        {
            ContentTree.Detach(inline);
        }

        items.Clear();
    }

    internal void AddBorrowed(Inline inline) => items.Add(inline);

    /// <inheritdoc/>
    public IEnumerator<Inline> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

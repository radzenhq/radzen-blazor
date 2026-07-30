using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the text runs in a paragraph or list item. A run belongs to exactly
/// one collection: adding a run that already has a parent throws.
/// </summary>
public sealed class InlineCollection : IReadOnlyList<Run>
{
    private readonly TrackedList<Run> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Run this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends a new run with the specified text.
    /// </summary>
    /// <param name="text">The run text.</param>
    /// <returns>The newly created run.</returns>
    public Run Add(string text) => Add(new Run(text));

    /// <summary>
    /// Appends an existing run.
    /// </summary>
    /// <param name="run">The run to append.</param>
    /// <returns>The same <paramref name="run"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="run"/> already belongs to another collection.</exception>
    public Run Add(Run run)
    {
        ContentTree.Attach(run, this);
        items.Add(run);
        return run;
    }

    /// <summary>
    /// Inserts a new run with the specified text at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="text">The run text.</param>
    /// <returns>The newly created run.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Run Insert(int index, string text) => Insert(index, new Run(text));

    /// <summary>
    /// Inserts an existing run at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="run">The run to insert.</param>
    /// <returns>The same <paramref name="run"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="run"/> already belongs to another collection.</exception>
    public Run Insert(int index, Run run)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(run, this);
        items.Insert(index, run);
        return run;
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
    /// Removes the specified run, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="run">The run to remove.</param>
    /// <returns><see langword="true"/> if the run was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> is <see langword="null"/>.</exception>
    public bool Remove(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (!items.Remove(run))
        {
            return false;
        }

        ContentTree.Detach(run);
        return true;
    }

    /// <summary>
    /// Removes the run at the specified position, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="index">The zero-based index of the run to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var run = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(run);
    }

    /// <summary>
    /// Moves the run at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// runs in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the run to move.</param>
    /// <param name="toIndex">The zero-based index the run ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>
    /// Removes every run, detaching each one so it may be added elsewhere.
    /// </summary>
    public void Clear()
    {
        foreach (var run in items)
        {
            ContentTree.Detach(run);
        }

        items.Clear();
    }

    internal void AddBorrowed(Run run) => items.Add(run);

    /// <inheritdoc/>
    public IEnumerator<Run> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

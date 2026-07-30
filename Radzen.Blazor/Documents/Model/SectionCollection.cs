using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the sections in a document. A section belongs to exactly one
/// document: adding a section that already has a parent throws.
/// </summary>
public sealed class SectionCollection : IReadOnlyList<Section>
{
    private readonly TrackedList<Section> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Section this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends a new section to the document.
    /// </summary>
    /// <returns>The newly created section.</returns>
    public Section Add() => Add(new Section());

    /// <summary>
    /// Appends an existing section, typically one previously removed from this or another document.
    /// </summary>
    /// <param name="section">The section to append.</param>
    /// <returns>The same <paramref name="section"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="section"/> already belongs to another document.</exception>
    public Section Add(Section section)
    {
        ContentTree.Attach(section, this);
        items.Add(section);
        return section;
    }

    /// <summary>
    /// Inserts a new section at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <returns>The newly created section.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Section Insert(int index) => Insert(index, new Section());

    /// <summary>
    /// Inserts an existing section at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="section">The section to insert.</param>
    /// <returns>The same <paramref name="section"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="section"/> already belongs to another document.</exception>
    public Section Insert(int index, Section section)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(section, this);
        items.Insert(index, section);
        return section;
    }

    /// <summary>
    /// Removes the specified section, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="section">The section to remove.</param>
    /// <returns><see langword="true"/> if the section was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    public bool Remove(Section section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (!items.Remove(section))
        {
            return false;
        }

        ContentTree.Detach(section);
        return true;
    }

    /// <summary>
    /// Removes the section at the specified position, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="index">The zero-based index of the section to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var section = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(section);
    }

    /// <summary>
    /// Moves the section at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// sections in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the section to move.</param>
    /// <param name="toIndex">The zero-based index the section ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>
    /// Removes every section, detaching each one so it may be added elsewhere.
    /// </summary>
    public void Clear()
    {
        foreach (var section in items)
        {
            ContentTree.Detach(section);
        }

        items.Clear();
    }

    /// <inheritdoc/>
    public IEnumerator<Section> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

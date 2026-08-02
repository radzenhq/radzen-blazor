using System;
using System.Collections;
using System.Collections.Generic;

using Radzen.Documents.Fonts;
using Radzen.Documents.Core;
namespace Radzen.Documents;


/// <summary>
/// A single table-of-contents line: the entry text, the anchor it navigates to and its
/// indentation level.
/// </summary>
public sealed class TocEntry
{
    /// <summary>
    /// Initializes a new <see cref="TocEntry"/>.
    /// </summary>
    /// <param name="text">The entry text shown on the line.</param>
    /// <param name="anchor">The anchor name the entry links to (see <see cref="Inline.Anchor"/>).</param>
    /// <param name="level">The zero-based indentation level. Defaults to 0.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="anchor"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is negative.</exception>
    public TocEntry(string text, string anchor, int level = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        Text = text;
        Anchor = anchor;
        Level = level;
    }

    /// <summary>Gets the entry text shown on the line.</summary>
    public string Text { get; }

    /// <summary>Gets the anchor name the entry links to.</summary>
    public string Anchor { get; }

    /// <summary>Gets the zero-based indentation level.</summary>
    public int Level { get; }

    internal object? Owner { get; set; }
}

/// <summary>
/// An ordered collection of the entries of a <see cref="TableOfContents"/>. An entry belongs to
/// exactly one table of contents: adding an entry that already has a parent throws.
/// </summary>
public sealed class TocEntryCollection : IReadOnlyList<TocEntry>
{
    internal TocEntryCollection()
    {
    }

    private readonly TrackedList<TocEntry> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public TocEntry this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends an existing entry.
    /// </summary>
    /// <param name="entry">The entry to append.</param>
    /// <returns>The same <paramref name="entry"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="entry"/> already belongs to a collection.</exception>
    public TocEntry Add(TocEntry entry)
    {
        ContentTree.Attach(entry, this);
        items.Add(entry);
        return entry;
    }

    /// <summary>
    /// Inserts an existing entry at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="entry">The entry to insert.</param>
    /// <returns>The same <paramref name="entry"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="entry"/> already belongs to a collection.</exception>
    public TocEntry Insert(int index, TocEntry entry)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(entry, this);
        items.Insert(index, entry);
        return entry;
    }

    /// <summary>
    /// Removes the specified entry, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    /// <returns><see langword="true"/> if the entry was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    public bool Remove(TocEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!items.Remove(entry))
        {
            return false;
        }

        ContentTree.Detach(entry);
        return true;
    }

    /// <summary>
    /// Removes the entry at the specified position, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="index">The zero-based index of the entry to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var entry = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(entry);
    }

    /// <summary>
    /// Moves the entry at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// entries in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the entry to move.</param>
    /// <param name="toIndex">The zero-based index the entry ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>
    /// Removes every entry, detaching each one so it may be added elsewhere.
    /// </summary>
    public void Clear()
    {
        foreach (var entry in items)
        {
            ContentTree.Detach(entry);
        }

        items.Clear();
    }

    /// <inheritdoc/>
    public IEnumerator<TocEntry> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// A table of contents rendered as one line per entry: the entry text, a leader of
/// <see cref="Leader"/> characters and the page the entry's anchor lands on, right-aligned in a
/// page-number column of fixed width. Every entry line is a clickable region that navigates to
/// its anchor. The page numbers are the ones the finished document paginates to, and the column
/// keeps the same width whatever the numbers turn out to be, so adding the table of contents does
/// not shift where the entries point. A table of contents is only supported as direct section
/// content.
/// </summary>
public sealed class TableOfContents : Block
{
    private Unit levelIndent = Unit.FromPoint(12);

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the entries, in the order their lines are rendered.</summary>
    public TocEntryCollection Entries { get; } = [];

    /// <summary>Gets the font applied to every entry line.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the leader character repeated between the entry text and the page number.
    /// Defaults to '.'.
    /// </summary>
    public char Leader { get; set; } = '.';

    /// <summary>
    /// Gets or sets the indentation applied per <see cref="TocEntry.Level"/>. Defaults to 12pt.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit LevelIndent
    {
        get => levelIndent;
        set => levelIndent = AuthoredNumber.Absolute(value, "TableOfContents.LevelIndent");
    }

    /// <summary>
    /// Adds an entry.
    /// </summary>
    /// <param name="text">The entry text shown on the line.</param>
    /// <param name="anchor">The anchor name the entry links to (see <see cref="Inline.Anchor"/>).</param>
    /// <param name="level">The zero-based indentation level. Defaults to 0.</param>
    /// <returns>The newly created entry.</returns>
    public TocEntry AddEntry(string text, string anchor, int level = 0)
        => Entries.Add(new TocEntry(text, anchor, level));
}

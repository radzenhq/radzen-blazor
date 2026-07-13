using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


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
    /// <param name="anchor">The anchor name the entry links to (see <see cref="Run.Anchor"/>).</param>
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
}

/// <summary>
/// A table of contents rendered as one line per entry: the entry text, a leader of
/// <see cref="Leader"/> characters and the resolved page number, right-aligned in a fixed
/// page-number column. Every entry line links to its anchor with an internal GoTo link.
/// Page numbers are resolved with a second layout pass; the page-number column footprint
/// is independent of the digits (sized for up to four), so both passes paginate identically.
/// </summary>
/// <remarks>
/// The block lowers to one <see cref="Paragraph"/> per entry (not a table) because internal
/// link annotations are only emitted for body paragraph lines: text inside table cells is
/// drawn by the table emitter, which produces no link annotations, so a two-column table
/// lowering would lose the required clickable links. Each paragraph carries a right-aligned
/// tab stop for the page number and a measured run of leader characters between the text and
/// the number. A table of contents is only supported as direct section content.
/// </remarks>
public sealed class TableOfContents : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the entries, in the order their lines are rendered.</summary>
    public IList<TocEntry> Entries { get; } = [];

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
    public Unit LevelIndent { get; set; } = Unit.FromPoint(12);

    /// <summary>
    /// Adds an entry.
    /// </summary>
    /// <param name="text">The entry text shown on the line.</param>
    /// <param name="anchor">The anchor name the entry links to (see <see cref="Run.Anchor"/>).</param>
    /// <param name="level">The zero-based indentation level. Defaults to 0.</param>
    /// <returns>The newly created entry.</returns>
    public TocEntry AddEntry(string text, string anchor, int level = 0)
    {
        var entry = new TocEntry(text, anchor, level);
        Entries.Add(entry);
        return entry;
    }
}

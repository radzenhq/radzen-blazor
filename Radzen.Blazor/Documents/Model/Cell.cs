using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A single table cell containing block-level content.
/// </summary>
public sealed class Cell
{
    private int columnSpan = 1;
    private int rowSpan = 1;

    internal Cell()
    {
    }

    /// <summary>Gets the block-level content of the cell.</summary>
    public BlockCollection Blocks { get; } = [];

    /// <summary>
    /// Gets or sets the cell text. Getting returns the text of the single paragraph when the cell
    /// contains exactly one paragraph, otherwise <see langword="null"/>. Setting replaces the content
    /// with a single paragraph holding that text.
    /// </summary>
    public string? Text
    {
        get => Blocks.Count == 1 && Blocks[0] is Paragraph paragraph ? paragraph.Text : null;
        set
        {
            Blocks.Clear();
            if (value != null)
            {
                Blocks.AddParagraph().Text = value;
            }
        }
    }

    /// <summary>Gets or sets the number of columns the cell spans. Must be at least 1. Defaults to 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1.</exception>
    public int ColumnSpan
    {
        get => columnSpan;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            columnSpan = value;
        }
    }

    /// <summary>Gets or sets the number of rows the cell spans. Must be at least 1. Defaults to 1.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than 1.</exception>
    public int RowSpan
    {
        get => rowSpan;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            rowSpan = value;
        }
    }

    /// <summary>Gets the default font for content in the cell.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the horizontal content alignment set directly on this cell, or <see langword="null"/>
    /// when none is set and the effective value comes from <see cref="StyleName"/>, the column or the row.
    /// Setting <see langword="null"/> resets it.
    /// </summary>
    public HorizontalAlignment? Alignment { get; set; }

    /// <summary>Gets or sets the vertical content alignment. Defaults to <see cref="VerticalAlignment.Top"/>.</summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>Gets or sets the padding applied on every edge of the cell.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit Padding
    {
        get => padding;
        set => padding = AuthoredNumber.Absolute(value, "Cell.Padding");
    }

    /// <summary>
    /// Gets or sets the left padding set directly on this edge, or <see langword="null"/> when none is set
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? PaddingLeft
    {
        get => paddingLeft;
        set => paddingLeft = AuthoredNumber.Absolute(value, "Cell.PaddingLeft");
    }

    /// <summary>
    /// Gets or sets the right padding set directly on this edge, or <see langword="null"/> when none is set
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? PaddingRight
    {
        get => paddingRight;
        set => paddingRight = AuthoredNumber.Absolute(value, "Cell.PaddingRight");
    }

    /// <summary>
    /// Gets or sets the top padding set directly on this edge, or <see langword="null"/> when none is set
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? PaddingTop
    {
        get => paddingTop;
        set => paddingTop = AuthoredNumber.Absolute(value, "Cell.PaddingTop");
    }

    /// <summary>
    /// Gets or sets the bottom padding set directly on this edge, or <see langword="null"/> when none is set
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? PaddingBottom
    {
        get => paddingBottom;
        set => paddingBottom = AuthoredNumber.Absolute(value, "Cell.PaddingBottom");
    }

    private Unit padding;
    private Unit? paddingLeft;
    private Unit? paddingRight;
    private Unit? paddingTop;
    private Unit? paddingBottom;

    internal Unit EffectivePaddingLeft => PaddingLeft ?? Padding;

    internal Unit EffectivePaddingRight => PaddingRight ?? Padding;

    internal Unit EffectivePaddingTop => PaddingTop ?? Padding;

    internal Unit EffectivePaddingBottom => PaddingBottom ?? Padding;

    /// <summary>
    /// Gets or sets the name of the applied named style, or <see langword="null"/> for none. The style
    /// supplies the font and the alignment this cell leaves unset, resolved as described on
    /// <see cref="Style"/>. A name that does not exist in <c>Document.Styles</c> fails at layout.
    /// </summary>
    public string? StyleName { get; set; }

    /// <summary>Gets or sets the cell background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>Gets the cell borders.</summary>
    public Borders Borders { get; } = new();
}

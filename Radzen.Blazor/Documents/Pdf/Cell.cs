using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A single table cell containing block-level content.
/// </summary>
public class Cell
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
            Blocks.AddParagraph().Text = value;
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

    private HorizontalAlignment? alignment;

    /// <summary>Gets or sets the horizontal content alignment. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment
    {
        get => alignment ?? HorizontalAlignment.Left;
        set => alignment = value;
    }

    internal HorizontalAlignment? AlignmentValue => alignment;

    /// <summary>Gets or sets the vertical content alignment. Defaults to <see cref="VerticalAlignment.Top"/>.</summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>Gets or sets the padding applied on every edge of the cell.</summary>
    public Unit Padding { get; set; }

    private Unit? paddingLeft;
    private Unit? paddingRight;
    private Unit? paddingTop;
    private Unit? paddingBottom;

    /// <summary>Gets or sets the left padding. Falls back to <see cref="Padding"/> when unset.</summary>
    public Unit PaddingLeft
    {
        get => paddingLeft ?? Padding;
        set => paddingLeft = value;
    }

    /// <summary>Gets or sets the right padding. Falls back to <see cref="Padding"/> when unset.</summary>
    public Unit PaddingRight
    {
        get => paddingRight ?? Padding;
        set => paddingRight = value;
    }

    /// <summary>Gets or sets the top padding. Falls back to <see cref="Padding"/> when unset.</summary>
    public Unit PaddingTop
    {
        get => paddingTop ?? Padding;
        set => paddingTop = value;
    }

    /// <summary>Gets or sets the bottom padding. Falls back to <see cref="Padding"/> when unset.</summary>
    public Unit PaddingBottom
    {
        get => paddingBottom ?? Padding;
        set => paddingBottom = value;
    }

    /// <summary>Gets or sets the name of the applied named style, or <see langword="null"/> for none.</summary>
    public string? StyleName { get; set; }

    /// <summary>Gets or sets the cell background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>Gets the cell borders.</summary>
    public Borders Borders { get; } = new();

    /// <summary>
    /// Gets or sets the corner radius of the cell box. When greater than zero the background
    /// is filled as a rounded rectangle (the effective radius is clamped to half of the smaller
    /// box dimension). The border is stroked as the same rounded path only when it is uniform -
    /// the same width, color and style resolve on all four edges; a non-uniform border keeps the
    /// square four-edge rendering while the background stays rounded. Defaults to 0 (square).
    /// </summary>
    public Unit CornerRadius { get; set; }
}

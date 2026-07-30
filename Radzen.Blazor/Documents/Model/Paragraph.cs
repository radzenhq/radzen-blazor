using System.Linq;

using Radzen.Documents.Fonts;
namespace Radzen.Documents;


/// <summary>
/// A block of inline text runs with paragraph-level formatting.
/// </summary>
public sealed class Paragraph : Block
{
    private double lineSpacing = 1.0;
    private int widows = 2;
    private int orphans = 2;

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the inline text runs.</summary>
    public InlineCollection Inlines { get; } = [];

    /// <summary>
    /// Gets or sets the paragraph text. Getting returns the concatenation of the <see cref="Run"/>
    /// texts, or <see langword="null"/> when the paragraph has no inline content; inlines that carry
    /// no authored text, such as images and page fields, contribute nothing. Setting
    /// <see langword="null"/> clears the inlines; setting a value replaces them all with a single
    /// run holding that text.
    /// </summary>
    public string? Text
    {
        get => Inlines.Count == 0
            ? null
            : string.Concat(Inlines.OfType<Run>().Select(static run => run.Text));
        set
        {
            Inlines.Clear();
            if (value != null)
            {
                Inlines.Add(value);
            }
        }
    }

    /// <summary>Gets the paragraph font.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the horizontal alignment set directly on this paragraph, or <see langword="null"/> when
    /// none is set and the effective value comes from the named style chain, the containing cell or the
    /// built-in default. Setting <see langword="null"/> resets it. Use
    /// <see cref="Document.Resolve(Paragraph)"/> for the effective value.
    /// </summary>
    public HorizontalAlignment? Alignment { get; set; }

    internal HorizontalAlignment ResolveAlignment(HorizontalAlignment? inherited)
        => Alignment ?? inherited ?? HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the left indent applied to every line of the paragraph, or <see langword="null"/> when
    /// none is set directly and the effective value comes from the named style chain or the built-in
    /// default. Setting <see langword="null"/> resets it. Use <see cref="Document.Resolve(Paragraph)"/> for
    /// the effective value.
    /// </summary>
    public Unit? LeftIndent { get; set; }

    /// <summary>
    /// Gets or sets the spacing before the paragraph, or <see langword="null"/> when none is set directly
    /// and the effective value comes from the named style chain or the built-in default. Setting
    /// <see langword="null"/> resets it. Use <see cref="Document.Resolve(Paragraph)"/> for the effective value.
    /// </summary>
    public Unit? SpacingBefore { get; set; }

    /// <summary>
    /// Gets or sets the spacing after the paragraph, or <see langword="null"/> when none is set directly
    /// and the effective value comes from the named style chain or the built-in default. Setting
    /// <see langword="null"/> resets it. Use <see cref="Document.Resolve(Paragraph)"/> for the effective value.
    /// </summary>
    public Unit? SpacingAfter { get; set; }

    /// <summary>Gets or sets the line spacing multiplier. Defaults to 1.0.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a finite number greater than zero.</exception>
    public double LineSpacing
    {
        get => lineSpacing;
        set => lineSpacing = AuthoredNumber.Positive(value, "Paragraph.LineSpacing");
    }

    /// <summary>
    /// Gets or sets the name of the applied named style. The style supplies the font and every formatting
    /// value this paragraph leaves unset, resolved as described on <see cref="Style"/>. A name that does
    /// not exist in <c>Document.Styles</c> fails at layout.
    /// </summary>
    public string? StyleName { get; set; }

    /// <summary>
    /// Gets the explicit tab stops. When any are defined, a '\t' advances to the next stop at or
    /// beyond the current position and the following text run is aligned per that stop. When empty,
    /// tabs keep the default 36pt left-tab grid.
    /// </summary>
    public TabStopCollection TabStops { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the paragraph is kept on a single page, or
    /// <see langword="null"/> when none is set directly and the effective value comes from the named style
    /// chain or the built-in default. Setting <see langword="null"/> resets it. Use
    /// <see cref="Document.Resolve(Paragraph)"/> for the effective value.
    /// </summary>
    public bool? KeepTogether { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the paragraph is kept with the next block, or
    /// <see langword="null"/> when none is set directly and the effective value comes from the named style
    /// chain or the built-in default. Setting <see langword="null"/> resets it. Use
    /// <see cref="Document.Resolve(Paragraph)"/> for the effective value.
    /// </summary>
    public bool? KeepWithNext { get; set; }

    /// <summary>Gets or sets the minimum number of lines left at the top of a page. Defaults to 2.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public int Widows
    {
        get => widows;
        set => widows = AuthoredNumber.NonNegative(value, "Paragraph.Widows");
    }

    /// <summary>Gets or sets the minimum number of lines left at the bottom of a page. Defaults to 2.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public int Orphans
    {
        get => orphans;
        set => orphans = AuthoredNumber.NonNegative(value, "Paragraph.Orphans");
    }
}

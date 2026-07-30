using System.Linq;

using Radzen.Documents.Fonts;
namespace Radzen.Documents;


/// <summary>
/// A block of inline text runs with paragraph-level formatting.
/// </summary>
public sealed class Paragraph : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the inline text runs.</summary>
    public InlineCollection Inlines { get; } = [];

    /// <summary>
    /// Gets or sets the paragraph text. Getting returns the concatenation of the run texts, or
    /// <see langword="null"/> when there are no runs. Setting <see langword="null"/> clears the runs;
    /// setting a value replaces all runs with a single run holding that text.
    /// </summary>
    public string? Text
    {
        get => Inlines.Count == 0 ? null : string.Concat(Inlines.Select(run => run.Text));
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
    public double LineSpacing { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the name of the applied named style. The style supplies the font and every formatting
    /// value this paragraph leaves unset, resolved as described on <see cref="Style"/>. A name that does
    /// not exist in <c>Document.Styles</c> fails at layout.
    /// </summary>
    public string? StyleName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the last tab character on a line advances to a
    /// right-aligned tab stop at the content-box right edge, making the text after it flush right
    /// on the same baseline. Earlier tabs keep the default left tab stops. Defaults to <see langword="false"/>.
    /// </summary>
    public bool RightTabStop { get; set; }

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
    public int Widows { get; set; } = 2;

    /// <summary>Gets or sets the minimum number of lines left at the bottom of a page. Defaults to 2.</summary>
    public int Orphans { get; set; } = 2;
}

using Radzen.Documents.Fonts;

namespace Radzen.Documents;


/// <summary>
/// A contiguous run of text sharing the same font. The inline hierarchy is closed: the set of
/// inline kinds is fixed by this library and cannot be extended from outside it. Create runs
/// through <see cref="InlineCollection.Add(string)"/>.
/// </summary>
public class Run
{
    private double opacity = 1;

    internal Run(string text) => Text = text;

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the hyperlink target URI. When set, every laid-out fragment of
    /// this run becomes a clickable region that opens the URI.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the internal link target. When set, every laid-out fragment of
    /// this run becomes a clickable region that navigates to the anchor of the
    /// same name (see <see cref="Anchor"/>).
    /// </summary>
    public string? LinkToAnchor { get; set; }

    /// <summary>
    /// Gets or sets the anchor name marking this run's position as a named
    /// navigation target. Outline entries targeting an anchor and internal links
    /// (<see cref="LinkToAnchor"/>) navigate to it.
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets the additional spacing added after each glyph of this run, in
    /// points. Included in measurement as spacing * (glyph count - 1).
    /// </summary>
    public Unit LetterSpacing { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment of this run. Superscript and subscript
    /// runs render at <see cref="VerticalAlignmentScale"/> of the font size, offset
    /// above or below the baseline.
    /// </summary>
    public RunVerticalAlignment VerticalAlignment { get; set; }

    /// <summary>
    /// Gets or sets the font-size scale applied when <see cref="VerticalAlignment"/> is
    /// <see cref="RunVerticalAlignment.Superscript"/> or <see cref="RunVerticalAlignment.Subscript"/>.
    /// </summary>
    public double VerticalAlignmentScale { get; set; } = 0.583;

    /// <summary>
    /// Gets or sets the opacity this run is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Run");
    }

    /// <summary>
    /// Gets or sets the additional spacing added at every word-separator space of this run, in
    /// points. Which positions count as word separators is determined by text shaping. Defaults
    /// to zero.
    /// </summary>
    public Unit WordSpacing { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scaling of the glyphs of this run, as a fraction of their
    /// natural width. Defaults to 1 (no scaling).
    /// </summary>
    public double HorizontalScale { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether this run is left unpainted. The glyphs still occupy space and
    /// remain available for selection and text extraction. Defaults to false.
    /// </summary>
    public bool Invisible { get; set; }

    internal object? Owner { get; set; }

    internal void CopyPropertiesTo(Run target)
    {
        target.Link = Link;
        target.LinkToAnchor = LinkToAnchor;
        target.Anchor = Anchor;
        target.LetterSpacing = LetterSpacing;
        target.VerticalAlignment = VerticalAlignment;
        target.VerticalAlignmentScale = VerticalAlignmentScale;
        target.Opacity = Opacity;
        target.WordSpacing = WordSpacing;
        target.HorizontalScale = HorizontalScale;
        target.Invisible = Invisible;
    }

    /// <summary>Gets the run font.</summary>
    public Font Font { get; } = new();

    internal double ScriptScale => VerticalAlignment == RunVerticalAlignment.None ? 1.0 : VerticalAlignmentScale;

    internal double ScriptRise(double size) => VerticalAlignment switch
    {
        RunVerticalAlignment.Superscript => size * 0.33,
        RunVerticalAlignment.Subscript => -size * 0.20,
        _ => 0.0,
    };
}

using Radzen.Documents.Fonts;

namespace Radzen.Documents;


/// <summary>
/// A piece of content that flows within a paragraph line. The inline hierarchy is closed: the set
/// of inline kinds is fixed by this library and cannot be extended from outside it. Add inlines
/// through <see cref="InlineCollection"/>.
/// </summary>
public abstract class Inline
{
    private double opacity = 1;

    private protected Inline()
    {
    }

    /// <summary>
    /// Gets or sets the hyperlink target URI. When set, this inline's laid-out area becomes a
    /// clickable region that opens the URI.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the internal link target. When set, this inline's laid-out area becomes a
    /// clickable region that navigates to the anchor of the same name (see <see cref="Anchor"/>).
    /// Ignored when <see cref="Link"/> is set.
    /// </summary>
    public string? LinkToAnchor { get; set; }

    /// <summary>
    /// Gets or sets the anchor name marking this inline's position as a named navigation target.
    /// Outline entries targeting an anchor and internal links (<see cref="LinkToAnchor"/>)
    /// navigate to it.
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets the opacity this inline is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Inline");
    }

    internal object? Owner { get; set; }
}

/// <summary>
/// Inline content drawn as glyphs, sharing the font and typographic settings every text inline
/// honors.
/// </summary>
public abstract class TextInline : Inline
{
    private double verticalAlignmentScale = 0.583;
    private double horizontalScale = 1.0;

    private protected TextInline()
    {
    }

    /// <summary>Gets the font of this inline.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the additional spacing added after each glyph, in points. Included in
    /// measurement as spacing * (glyph count - 1).
    /// </summary>
    public Unit LetterSpacing { get; set; }

    /// <summary>
    /// Gets or sets the additional spacing added at every word-separator space, in points. Which
    /// positions count as word separators is determined by text shaping. Defaults to zero.
    /// </summary>
    public Unit WordSpacing { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment of this inline. Superscript and subscript inlines
    /// render at <see cref="VerticalAlignmentScale"/> of the font size, offset above or below
    /// the baseline.
    /// </summary>
    public RunVerticalAlignment VerticalAlignment { get; set; }

    /// <summary>
    /// Gets or sets the font-size scale applied when <see cref="VerticalAlignment"/> is
    /// <see cref="RunVerticalAlignment.Superscript"/> or <see cref="RunVerticalAlignment.Subscript"/>.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a finite number greater than zero.</exception>
    public double VerticalAlignmentScale
    {
        get => verticalAlignmentScale;
        set => verticalAlignmentScale = AuthoredNumber.Positive(value, "TextInline.VerticalAlignmentScale");
    }

    /// <summary>
    /// Gets or sets the horizontal scaling of the glyphs, as a fraction of their natural width.
    /// Defaults to 1 (no scaling).
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not a finite number greater than zero.</exception>
    public double HorizontalScale
    {
        get => horizontalScale;
        set => horizontalScale = AuthoredNumber.Positive(value, "TextInline.HorizontalScale");
    }

    /// <summary>
    /// Gets or sets whether this inline is left unpainted. The glyphs still occupy space and
    /// remain available for selection and text extraction. Defaults to false.
    /// </summary>
    public bool Invisible { get; set; }

    internal abstract string LayoutText { get; }

    internal void CopyPropertiesTo(TextInline target)
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

    internal double ScriptScale => VerticalAlignment == RunVerticalAlignment.None ? 1.0 : VerticalAlignmentScale;

    internal double ScriptRise(double size) => VerticalAlignment switch
    {
        RunVerticalAlignment.Superscript => size * 0.33,
        RunVerticalAlignment.Subscript => -size * 0.20,
        _ => 0.0,
    };
}

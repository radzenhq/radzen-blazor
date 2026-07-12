namespace Radzen.Documents.Pdf;


/// <summary>
/// A contiguous run of text sharing the same font.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="Run"/> with the specified text.
/// </remarks>
/// <param name="text">The run text.</param>
public class Run(string text)
{

    /// <summary>Gets or sets the run text.</summary>
    public string Text { get; set; } = text;

    /// <summary>
    /// Gets or sets the hyperlink target URI. When set, every laid-out fragment of
    /// this run is covered by a link annotation opening the URI.
    /// </summary>
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the internal link target. When set, every laid-out fragment of
    /// this run is covered by a link annotation that jumps to the anchor of the
    /// same name (see <see cref="Anchor"/>).
    /// </summary>
    public string? LinkToAnchor { get; set; }

    /// <summary>
    /// Gets or sets the anchor name marking this run's position as a named
    /// destination. Outline entries (<see cref="OutlineTarget.ToAnchor"/>) and
    /// internal links (<see cref="LinkToAnchor"/>) navigate to it.
    /// </summary>
    public string? Anchor { get; set; }

    /// <summary>
    /// Gets or sets the extra spacing inserted between the glyphs of this run.
    /// Rendered with the PDF character-spacing (Tc) operator and included in
    /// measurement as spacing * (glyph count - 1).
    /// </summary>
    public Unit LetterSpacing { get; set; }

    /// <summary>
    /// Gets or sets the vertical alignment of this run. Superscript and subscript
    /// runs render at <see cref="VerticalAlignScale"/> of the font size, raised or
    /// lowered via the PDF text-rise (Ts) operator.
    /// </summary>
    public RunVerticalAlign VerticalAlign { get; set; }

    /// <summary>
    /// Gets or sets the font-size scale applied when <see cref="VerticalAlign"/> is
    /// <see cref="RunVerticalAlign.Superscript"/> or <see cref="RunVerticalAlign.Subscript"/>.
    /// </summary>
    public double VerticalAlignScale { get; set; } = 0.583;

    /// <summary>Gets the run font.</summary>
    public Font Font { get; } = new();

    internal Font? EffectiveFont { get; set; }

    internal Font ResolvedFont => EffectiveFont ?? Font;

    internal double ScriptScale => VerticalAlign == RunVerticalAlign.None ? 1.0 : VerticalAlignScale;

    // Rise fractions of the ORIGINAL font size, matching common typesetting defaults.
    internal double ScriptRise(double size) => VerticalAlign switch
    {
        RunVerticalAlign.Superscript => size * 0.33,
        RunVerticalAlign.Subscript => -size * 0.20,
        _ => 0.0,
    };
}

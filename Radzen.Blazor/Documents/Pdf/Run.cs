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

    /// <summary>
    /// Gets or sets the opacity this run is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    public double Opacity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the extra spacing added to every ASCII-space (byte 32) glyph in this
    /// run, rendered with the PDF word-spacing (Tw) operator. Defaults to zero.
    /// </summary>
    public Unit WordSpacing { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scaling of this run as a percentage, rendered with the
    /// PDF horizontal-scaling (Tz) operator. Defaults to 100 (no scaling).
    /// </summary>
    public double HorizontalScale { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether this run is drawn invisibly (text rendering mode 3). The glyphs
    /// still occupy space and remain selectable/searchable but paint nothing. Defaults to false.
    /// </summary>
    public bool Invisible { get; set; }

    // A non-RGB device fill colour set through the SetFill* helpers; overrides the font
    // colour's rg emission with g/k/scn. Null (default) keeps the rg path byte-identical.
    internal DeviceColor? FillPaint { get; private set; }

    /// <summary>
    /// Sets the run fill colour to a DeviceGray level (the <c>g</c> operator), 0 (black)
    /// to 1 (white). Overrides the font colour for filling.
    /// </summary>
    /// <param name="gray">The gray level, clamped to 0..1.</param>
    public void SetFillGray(double gray)
        => FillPaint = new DeviceColor(DeviceColorKind.Gray, null, [ClampUnit(gray)]);

    /// <summary>
    /// Sets the run fill colour to a DeviceCMYK colour (the <c>k</c> operator). Each
    /// component is clamped to 0..1. Overrides the font colour for filling.
    /// </summary>
    /// <param name="cyan">The cyan component.</param>
    /// <param name="magenta">The magenta component.</param>
    /// <param name="yellow">The yellow component.</param>
    /// <param name="black">The black (key) component.</param>
    public void SetFillCmyk(double cyan, double magenta, double yellow, double black)
        => FillPaint = new DeviceColor(
            DeviceColorKind.Cmyk, null, [ClampUnit(cyan), ClampUnit(magenta), ClampUnit(yellow), ClampUnit(black)]);

    private static double ClampUnit(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    /// <summary>Gets the run font.</summary>
    public Font Font { get; } = new();

    internal double ScriptScale => VerticalAlign == RunVerticalAlign.None ? 1.0 : VerticalAlignScale;

    // Rise fractions of the ORIGINAL font size, matching common typesetting defaults.
    internal double ScriptRise(double size) => VerticalAlign switch
    {
        RunVerticalAlign.Superscript => size * 0.33,
        RunVerticalAlign.Subscript => -size * 0.20,
        _ => 0.0,
    };
}

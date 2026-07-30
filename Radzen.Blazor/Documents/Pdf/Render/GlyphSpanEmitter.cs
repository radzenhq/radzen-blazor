using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal readonly struct EmittedGlyphSpan
{
    public required EmittedFont Font { get; init; }

    public required byte[] Bytes { get; init; }

    public double[]? Kerns { get; init; }

    public SfntFont? Face { get; init; }
}

internal sealed class GlyphSpanEmitter(
    GeneratorFontResolver fontResolver,
    bool allowUnsupportedCharacters)
{
    private readonly SfntRunBuilder sfnt = new(fontResolver);
    private readonly Base14GlyphEncoder base14 = new(allowUnsupportedCharacters);

    public EmittedGlyphSpan Emit(in CapturedGlyphSpan span, double emSize)
    {
        if (span.Face.Kind == CapturedFontFaceKind.Sfnt)
        {
            var run = sfnt.Build(span, emSize);
            return new EmittedGlyphSpan
            {
                Font = run.Font,
                Face = run.Face,
                Bytes = run.Bytes,
                Kerns = run.Kerns,
            };
        }

        var face = span.Face.BuiltIn;
        var glyphs = span.BuiltInGlyphs;
        var kerns = glyphs.Length > 1 ? new double[glyphs.Length - 1] : [];
        for (var i = 0; i < kerns.Length; i++)
        {
            kerns[i] = SfntRunBuilder.PdfTextAdjustment(
                glyphs[i].TextAdjustmentPoints,
                emSize);
        }

        return new EmittedGlyphSpan
        {
            Font = fontResolver.ResolveBase14(face),
            Bytes = base14.Encode(glyphs, face),
            Kerns = SfntRunBuilder.HasNonZero(kerns) ? kerns : null,
        };
    }
}

using Radzen.Documents.Fonts;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Render;

internal readonly struct EmittedGlyphSpan
{
    public required EmittedFont Font { get; init; }

    public required byte[] Bytes { get; init; }

    public double[]? Kerns { get; init; }

    public SfntFont? Face { get; init; }
}

internal sealed class GlyphSpanRecorder(
    FontRegistry fontRegistry,
    UnsupportedCharacterPolicy unsupportedCharacters,
    UnsupportedCharacterLog unsupported)
{
    private readonly SfntGlyphEncoder sfnt = new(fontRegistry, unsupported);
    private readonly Base14GlyphEncoder base14 = new(unsupportedCharacters, unsupported);

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
        var glyphs = span.Glyphs;
        var kerns = glyphs.Length > 1 ? new double[glyphs.Length - 1] : [];
        for (var i = 0; i < kerns.Length; i++)
        {
            kerns[i] = SfntGlyphEncoder.PdfTextAdjustment(glyphs[i].Kerning, emSize);
        }

        return new EmittedGlyphSpan
        {
            Font = fontRegistry.ResolveBase14(face),
            Bytes = base14.Encode(glyphs, face),
            Kerns = SfntGlyphEncoder.HasNonZero(kerns) ? kerns : null,
        };
    }
}

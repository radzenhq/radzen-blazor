using System;
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Emit;

// Owns the per-document font caches: a stable GeneratedFont per base-14 face and per
// embedded sfnt face, keyed so a face registered once is emitted once. Also carries the
// WinAnsi/codepoint helpers the emitters share.
internal sealed class GeneratorFontResolver(PdfAConformance conformance)
{
    private readonly List<GeneratedFont> allFonts = [];
    private readonly Dictionary<string, GeneratedFont> base14Fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<SfntFont, GeneratedFont> sfntFonts = [];

    public IReadOnlyList<GeneratedFont> AllFonts => allFonts;

    public GeneratedFont ResolveSfnt(SfntFont sfnt)
    {
        if (sfntFonts.TryGetValue(sfnt, out var existing))
        {
            return existing;
        }

        var generated = new GeneratedFont { Key = "F" + allFonts.Count.ToString(CultureInfo.InvariantCulture), Sfnt = sfnt };
        sfntFonts[sfnt] = generated;
        allFonts.Add(generated);
        return generated;
    }

    public GeneratedFont ResolveBase14(Font font)
    {
        var name = Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";
        if (conformance != PdfAConformance.None)
        {
            throw new InvalidOperationException(
                $"PDF/A forbids the standard-14 font '{name}' referenced by name; register an embeddable font file for '{font.Name}' with DocumentBuilder.Fonts instead.");
        }

        if (base14Fonts.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var generated = new GeneratedFont { Key = "F" + allFonts.Count.ToString(CultureInfo.InvariantCulture), Base14 = name };
        base14Fonts[name] = generated;
        allFonts.Add(generated);
        return generated;
    }

    public static int CodePointAt(string text, int index) => FontCollection.CodePointAt(text, index);

    public static bool IsWinAnsi(int codepoint)
        => codepoint <= 0xFFFF && WinAnsiEncoding.TryGetCode((char)codepoint, out _);

    public static byte[] EncodeWinAnsi(string text)
    {
        var bytes = new List<byte>(text.Length);
        foreach (var c in text)
        {
            if (WinAnsiEncoding.TryGetCode(c, out var code))
            {
                bytes.Add(code);
            }
        }

        return [.. bytes];
    }

    // Reverse maps for fresh (unsaved) text extraction: embedded Type0 fonts decode
    // their glyph-id codes through the accumulated gid-to-Unicode table, mirroring
    // the /ToUnicode CMap the embedder writes on save.
    // The maps are derived from document-global state frozen before the page loop, so each
    // font's ReverseFont is built once and shared by every page that references it.
    public static Dictionary<string, ReverseFont> BuildExtractionFonts(GeneratedPage generated)
    {
        var map = new Dictionary<string, ReverseFont>(StringComparer.Ordinal);
        foreach (var font in generated.Fonts)
        {
            map[font.Key] = font.Extraction ??=
                font.Sfnt is null ? ReverseFont.WinAnsi : ReverseFont.FromGlyphIds(RemapGidToUnicode(font));
        }

        return map;
    }

    private static Dictionary<ushort, int> RemapGidToUnicode(GeneratedFont font)
        => font.CompactGidMap is { } gidMap
            ? Fonts.Type0FontEmbedder.RemapToCompactGids(font.GidToUnicode, gidMap)
            : font.GidToUnicode;
}

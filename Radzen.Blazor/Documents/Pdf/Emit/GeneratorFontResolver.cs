using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Emit;

// Owns the per-document font caches: a stable GeneratedFont per base-14 face and per
// embedded sfnt face, keyed so a face registered once is emitted once. Also carries the
// WinAnsi/codepoint helpers the emitters share.
internal sealed class GeneratorFontResolver(PdfAConformance conformance)
{
    // One registry over both identity domains: a base-14 face is identified by its
    // PostScript name and an embedded face by instance, but they share one key sequence.
    private readonly ResourceKeyRegistry<object, GeneratedFont> fonts = new("F");

    public IReadOnlyList<GeneratedFont> AllFonts => fonts.Values;

    public GeneratedFont ResolveSfnt(SfntFont sfnt)
        => fonts.GetOrAddValue(sfnt, key => new GeneratedFont { Key = key, Sfnt = sfnt });

    public GeneratedFont ResolveBase14(Font font)
    {
        var name = FontResolution.ResolveBase14Name(font);
        if (conformance != PdfAConformance.None)
        {
            throw FontResolution.Base14Forbidden("PDF/A", name, font.Name);
        }

        return fonts.GetOrAddValue(name, key => new GeneratedFont { Key = key, Base14 = name });
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

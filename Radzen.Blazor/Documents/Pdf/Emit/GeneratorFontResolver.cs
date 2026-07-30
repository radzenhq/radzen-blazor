using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class GeneratorFontResolver(PdfAConformance conformance)
{
    private readonly ResourceKeyRegistry<object, GeneratedFont> fonts = new("F");

    public IReadOnlyList<GeneratedFont> AllFonts => fonts.Values;

    public GeneratedFont ResolveSfnt(SfntFont sfnt)
        => fonts.GetOrAddValue(sfnt, key => new GeneratedFont { Key = key, Sfnt = sfnt });

    private FontScope Scope => new(
        Fonts: null,
        Snapshot: null,
        conformance != PdfAConformance.None ? "PDF/A" : null,
        CanEmbed: true);

    public GeneratedFont ResolveBase14(CapturedBuiltInFace face)
    {
        var name = face.PostScriptName;
        if (Scope.Base14ForbiddenBy is { } label)
        {
            throw FontResolution.Base14Forbidden(label, name, family: null);
        }

        return fonts.GetOrAddValue(name, key => new GeneratedFont { Key = key, Base14 = name });
    }

    public static int CodePointAt(string text, int index) => FontCollection.CodePointAt(text, index);

    public static int CodePointAt(string text, int index, out int length)
        => FontCollection.CodePointAt(text, index, out length);

    public static bool IsWinAnsi(int codepoint)
        => codepoint <= 0xFFFF && WinAnsiEncoding.TryGetCode((char)codepoint, out _);

    public static Dictionary<string, ReverseFont> BuildExtractionFonts(GeneratedPage generated)
    {
        var map = new Dictionary<string, ReverseFont>(StringComparer.Ordinal);
        foreach (var font in generated.Fonts)
        {
            map[font.Key] = font.Extraction ??=
                font.Sfnt is null ? ReverseFont.FromBase14(font.Base14Name) : ReverseFont.FromGlyphIds(RemapGidToUnicode(font));
        }

        return map;
    }

    private static Dictionary<ushort, int> RemapGidToUnicode(GeneratedFont font)
        => font.CompactGidMap is { } gidMap
            ? Fonts.Type0FontEmbedder.RemapToCompactGids(font.GidToUnicode, gidMap)
            : font.GidToUnicode;
}

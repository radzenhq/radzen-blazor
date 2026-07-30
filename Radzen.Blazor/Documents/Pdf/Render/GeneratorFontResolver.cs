using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class EmittedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    public string Base14Name => Base14 ?? "Helvetica";

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

    public Dictionary<ushort, ushort>? CompactGidMap { get; set; }
}

internal sealed class GeneratorFontResolver(PdfAConformance conformance)
{
    private readonly ResourceNameAllocator<object, EmittedFont> fonts = new("F");

    public EmittedFont ResolveSfnt(SfntFont sfnt)
        => fonts.GetOrAddValue(sfnt, key => new EmittedFont { Key = key, Sfnt = sfnt });

    private FontScope Scope => new(
        Fonts: null,
        Snapshot: null,
        conformance != PdfAConformance.None ? "PDF/A" : null,
        CanEmbed: true);

    public EmittedFont ResolveBase14(CapturedBuiltInFace face)
    {
        var name = face.PostScriptName;
        if (Scope.Base14ForbiddenBy is { } label)
        {
            throw FontResolution.Base14Forbidden(label, name, family: null);
        }

        return fonts.GetOrAddValue(name, key => new EmittedFont { Key = key, Base14 = name });
    }

    public static int CodePointAt(string text, int index) => FontCollection.CodePointAt(text, index);

    public static int CodePointAt(string text, int index, out int length)
        => FontCollection.CodePointAt(text, index, out length);

    public Dictionary<EmittedFont, EmissionFont> Plan()
    {
        var planned = new Dictionary<EmittedFont, EmissionFont>(fonts.Count);
        foreach (var font in fonts.Values)
        {
            planned.Add(font, Plan(font));
        }

        return planned;
    }

    private static EmissionFont Plan(EmittedFont font)
    {
        if (font.Sfnt is not { } sfnt)
        {
            return new EmissionFont(font.Key, font.Base14, null, ReverseFont.FromBase14(font.Base14Name));
        }

        var gidMap = Fonts.CompactGidMap.Build(sfnt, font.GidToUnicode.Keys);
        font.CompactGidMap = gidMap;
        return new EmissionFont(
            font.Key,
            font.Base14,
            Type0FontPlanner.Plan(sfnt, font.GidToUnicode, gidMap),
            ReverseFont.FromGlyphIds(Type0FontEmbedder.RemapToCompactGids(font.GidToUnicode, gidMap)));
    }

    public static Dictionary<string, ReverseFont> ExtractionFonts(PageEmissionPlan page)
    {
        var map = new Dictionary<string, ReverseFont>(StringComparer.Ordinal);
        foreach (var font in page.Fonts)
        {
            map[font.Key] = font.Extraction;
        }

        return map;
    }
}

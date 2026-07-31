using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Fonts;

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
    private readonly Dictionary<string, ReverseFont> extraction = new(StringComparer.Ordinal);

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

    public Dictionary<EmittedFont, OutputFont> Plan()
    {
        var planned = new Dictionary<EmittedFont, OutputFont>(fonts.Count);
        foreach (var font in fonts.Values)
        {
            planned.Add(font, Plan(font));
        }

        return planned;
    }

    private OutputFont Plan(EmittedFont font)
    {
        if (font.Sfnt is not { } sfnt)
        {
            extraction[font.Key] = ReverseFont.FromBase14(font.Base14Name);
            return new OutputFont(font.Key, font.Base14, null);
        }

        var gidMap = Fonts.CompactGidMap.Build(sfnt, font.GidToUnicode.Keys);
        font.CompactGidMap = gidMap;
        extraction[font.Key] = ReverseFont.FromGlyphIds(
            Type0FontEmbedder.RemapToCompactGids(font.GidToUnicode, gidMap));
        return new OutputFont(
            font.Key,
            font.Base14,
            Type0FontPlanner.Plan(sfnt, font.GidToUnicode, gidMap));
    }

    public Dictionary<string, ReverseFont> ExtractionFonts(PageOutput page)
    {
        var map = new Dictionary<string, ReverseFont>(StringComparer.Ordinal);
        foreach (var font in page.Fonts)
        {
            map[font.Key] = extraction[font.Key];
        }

        return map;
    }
}

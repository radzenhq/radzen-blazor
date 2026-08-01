using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Content;

namespace Radzen.Documents.Pdf.Render;

internal sealed class EmittedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    public string Base14Name => Base14 ?? "Helvetica";

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

}

internal sealed record PlannedFont(OutputFont Output, IReadOnlyDictionary<ushort, ushort>? CompactGidMap);

internal sealed class FontRegistry
{
    private readonly ResourceNameAllocator<object, EmittedFont> fonts = new(ContentResourcePrefixes.Page.Font);
    private readonly Dictionary<string, ReverseFont> extraction = new(StringComparer.Ordinal);

    public EmittedFont ResolveSfnt(SfntFont sfnt)
        => fonts.GetOrAddValue(sfnt, key => new EmittedFont { Key = key, Sfnt = sfnt });

    public EmittedFont ResolveBase14(CapturedBuiltInFace face)
    {
        var name = StandardFonts.PostScriptName(face);
        return fonts.GetOrAddValue(name, key => new EmittedFont { Key = key, Base14 = name });
    }

    public Dictionary<EmittedFont, PlannedFont> Plan()
    {
        var planned = new Dictionary<EmittedFont, PlannedFont>(fonts.Count);
        foreach (var font in fonts.Values)
        {
            planned.Add(font, Plan(font));
        }

        return planned;
    }

    private PlannedFont Plan(EmittedFont font)
    {
        if (font.Sfnt is not { } sfnt)
        {
            extraction[font.Key] = ReverseFont.FromBase14(font.Base14Name);
            return new PlannedFont(new OutputFont(font.Key, font.Base14, null), null);
        }

        var gidMap = Fonts.CompactGidMap.Build(sfnt, font.GidToUnicode.Keys);
        extraction[font.Key] = ReverseFont.FromGlyphIds(
            Type0FontEmbedder.RemapToCompactGids(font.GidToUnicode, gidMap));
        return new PlannedFont(
            new OutputFont(
                font.Key,
                font.Base14,
                Type0FontPlanner.Plan(sfnt, font.GidToUnicode, gidMap)),
            gidMap);
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

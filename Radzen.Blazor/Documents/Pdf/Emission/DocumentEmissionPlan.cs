using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Geometry;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class DocumentEmissionPlan(
    ImmutableArray<PageEmissionPlan> pages,
    StructureElementSnapshot? structure,
    ImmutableDictionary<string, EmissionAnchor> anchors,
    ImmutableArray<KeyValuePair<string, string>> roleMap)
{
    public ImmutableArray<PageEmissionPlan> Pages { get; } = pages;

    public StructureElementSnapshot? Structure { get; } = structure;

    public ImmutableDictionary<string, EmissionAnchor> Anchors { get; } = anchors;

    public ImmutableArray<KeyValuePair<string, string>> RoleMap { get; } = roleMap;
}

internal sealed class PageEmissionPlan(
    ReadOnlyMemory<byte> content,
    ImmutableArray<EmissionFont> fonts,
    ImmutableArray<EmissionImage> images,
    ImmutableArray<EmissionLink> links,
    ImmutableArray<EmissionExtGState> extGStates,
    ImmutableArray<EmissionPattern> patterns)
{
    private readonly byte[] content = content.ToArray();

    public ReadOnlyMemory<byte> Content => content;

    public ImmutableArray<EmissionFont> Fonts { get; } = fonts;

    public ImmutableArray<EmissionImage> Images { get; } = images;

    public ImmutableArray<EmissionLink> Links { get; } = links;

    public ImmutableArray<EmissionExtGState> ExtGStates { get; } = extGStates;

    public ImmutableArray<EmissionPattern> Patterns { get; } = patterns;
}

internal sealed class EmissionFont(
    string key,
    string? base14,
    SfntFont? sfnt,
    ImmutableDictionary<ushort, int> gidToUnicode,
    ImmutableDictionary<ushort, ushort>? compactGidMap,
    ReverseFont extraction)
{
    public string Key { get; } = key;

    public string? Base14 { get; } = base14;

    public string Base14Name => Base14 ?? "Helvetica";

    public SfntFont? Sfnt { get; } = sfnt;

    public ImmutableDictionary<ushort, int> GidToUnicode { get; } = gidToUnicode;

    public ImmutableDictionary<ushort, ushort>? CompactGidMap { get; } = compactGidMap;

    public ReverseFont Extraction { get; } = extraction;
}

internal sealed class EmissionImage(
    string key,
    EmissionStreamSnapshot image,
    EmissionStreamSnapshot? softMask)
{
    public string Key { get; } = key;

    public EmissionStreamSnapshot Image { get; } = image;

    public EmissionStreamSnapshot? SoftMask { get; } = softMask;
}

internal sealed class EmissionImageResource(
    object identity,
    EmissionStreamSnapshot image,
    EmissionStreamSnapshot? softMask)
{
    public object Identity { get; } = identity;

    public EmissionStreamSnapshot Image { get; } = image;

    public EmissionStreamSnapshot? SoftMask { get; } = softMask;
}

internal readonly record struct EmissionLink(
    double X1,
    double Y1,
    double X2,
    double Y2,
    string? Uri,
    string? Destination,
    int? StructureElementId);

internal readonly record struct EmissionAnchor(PageEmissionPlan Page, double Top);

internal sealed class EmissionExtGState(
    string key,
    double fillAlpha,
    double strokeAlpha,
    BlendMode? blend,
    EmissionSoftMask? softMask,
    bool clearSoftMask)
{
    public string Key { get; } = key;

    public double FillAlpha { get; } = fillAlpha;

    public double StrokeAlpha { get; } = strokeAlpha;

    public BlendMode? Blend { get; } = blend;

    public EmissionSoftMask? SoftMask { get; } = softMask;

    public bool ClearSoftMask { get; } = clearSoftMask;
}

internal sealed class EmissionPattern(string key, EmissionDictionarySnapshot pattern)
{
    public string Key { get; } = key;

    public EmissionDictionarySnapshot Pattern { get; } = pattern;
}

internal enum EmissionSoftMaskType
{
    Alpha,
    Luminosity,
}

internal sealed class EmissionSoftMask(
    EmissionSoftMaskType type,
    EmissionTransparencyGroup group,
    ImmutableArray<double>? backdrop)
{
    public EmissionSoftMaskType Type { get; } = type;

    public EmissionTransparencyGroup Group { get; } = group;

    public ImmutableArray<double>? Backdrop { get; } = backdrop;
}

internal sealed class EmissionTransparencyGroup(
    ReadOnlyMemory<byte> content,
    ImmutableArray<double> boundingBox,
    string? colorSpace,
    bool? isolated,
    bool? knockout,
    ImmutableArray<KeyValuePair<string, EmissionStreamSnapshot>> xObjects)
{
    private readonly byte[] content = content.ToArray();

    public ReadOnlyMemory<byte> Content => content;

    public ImmutableArray<double> BoundingBox { get; } = boundingBox;

    public string? ColorSpace { get; } = colorSpace;

    public bool? Isolated { get; } = isolated;

    public bool? Knockout { get; } = knockout;

    public ImmutableArray<KeyValuePair<string, EmissionStreamSnapshot>> XObjects { get; } = xObjects;
}

internal readonly record struct StructureKidSnapshot(
    StructureElementSnapshot? Child,
    PageEmissionPlan? Page,
    int Mcid);

internal sealed class StructureElementSnapshot(
    int id,
    string type,
    string? alt,
    string? actualText,
    SemanticHeaderScope headerScope,
    int rowSpan,
    int columnSpan,
    ImmutableArray<StructureElementSnapshot> children,
    ImmutableArray<(PageEmissionPlan Page, int Mcid)> marks,
    ImmutableArray<StructureKidSnapshot> kids)
{
    public int Id { get; } = id;

    public string Type { get; } = type;

    public string? Alt { get; } = alt;

    public string? ActualText { get; } = actualText;

    public SemanticHeaderScope HeaderScope { get; } = headerScope;

    public int RowSpan { get; } = rowSpan;

    public int ColumnSpan { get; } = columnSpan;

    public ImmutableArray<StructureElementSnapshot> Children { get; } = children;

    public ImmutableArray<(PageEmissionPlan Page, int Mcid)> Marks { get; } = marks;

    public ImmutableArray<StructureKidSnapshot> Kids { get; } = kids;
}

internal sealed class EmissionStreamSnapshot
{
    private readonly byte[] data;
    private readonly ImmutableArray<KeyValuePair<string, DocumentObject>> entries;

    private EmissionStreamSnapshot(
        ReadOnlyMemory<byte> data,
        ImmutableArray<KeyValuePair<string, DocumentObject>> entries)
    {
        this.data = data.ToArray();
        this.entries = entries;
    }

    public static EmissionStreamSnapshot Capture(StreamObject stream)
        => new(stream.Data, [.. stream.Dictionary]);

    public bool TryGetValue(string key, out DocumentObject? value)
    {
        foreach (var entry in entries)
        {
            if (string.Equals(entry.Key, key, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public StreamObject CreateStream()
    {
        var stream = new StreamObject(data);
        foreach (var entry in entries)
        {
            stream.Dictionary[entry.Key] = entry.Value;
        }

        return stream;
    }
}

internal sealed class EmissionDictionarySnapshot
{
    private readonly ImmutableArray<KeyValuePair<string, DocumentObject>> entries;

    private EmissionDictionarySnapshot(ImmutableArray<KeyValuePair<string, DocumentObject>> entries)
        => this.entries = entries;

    public static EmissionDictionarySnapshot Capture(DictionaryObject dictionary) => new([.. dictionary]);

    public DictionaryObject CreateDictionary()
    {
        var dictionary = new DictionaryObject();
        foreach (var entry in entries)
        {
            dictionary[entry.Key] = entry.Value;
        }

        return dictionary;
    }
}

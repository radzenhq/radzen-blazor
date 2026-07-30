using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class DocumentEmissionPlan(
    ImmutableArray<PageEmissionPlan> pages,
    StructureElementSnapshot? structure,
    ImmutableDictionary<string, EmissionAnchor> anchors,
    ImmutableArray<KeyValuePair<string, string>> roleMap,
    ImmutableArray<string> unmappedRoles)
{
    public ImmutableArray<PageEmissionPlan> Pages { get; } = pages;

    public StructureElementSnapshot? Structure { get; } = structure;

    public ImmutableDictionary<string, EmissionAnchor> Anchors { get; } = anchors;

    public ImmutableArray<KeyValuePair<string, string>> RoleMap { get; } = roleMap;

    public ImmutableArray<string> UnmappedRoles { get; } = unmappedRoles;
}

internal sealed class PageEmissionPlan(
    byte[] content,
    ImmutableArray<EmissionFont> fonts,
    ImmutableArray<EmissionImage> images,
    ImmutableArray<EmissionLink> links,
    ImmutableArray<EmissionExtGState> extGStates,
    ImmutableArray<EmissionPattern> patterns)
{
    public ReadOnlyMemory<byte> Content => ContentArray;

    public byte[] ContentArray { get; } = content;

    public ImmutableArray<EmissionFont> Fonts { get; } = fonts;

    public ImmutableArray<EmissionImage> Images { get; } = images;

    public ImmutableArray<EmissionLink> Links { get; } = links;

    public ImmutableArray<EmissionExtGState> ExtGStates { get; } = extGStates;

    public ImmutableArray<EmissionPattern> Patterns { get; } = patterns;
}

internal enum EmissionFontFileKind
{
    Glyf,
    Cff,
}

internal readonly record struct EmissionWidthRun(int Cid, ImmutableArray<int> Widths);

internal sealed class EmissionFontProgram(
    EmissionFontFileKind kind,
    byte[] file,
    byte[] cidSet,
    ImmutableArray<EmissionWidthRun> widths,
    byte[] toUnicode,
    string baseName,
    int flags,
    ImmutableArray<int> boundingBox,
    double italicAngle,
    int ascent,
    int descent,
    int capHeight)
{
    public EmissionFontFileKind Kind { get; } = kind;

    public ReadOnlyMemory<byte> File { get; } = file;

    public ReadOnlyMemory<byte> CidSet { get; } = cidSet;

    public ImmutableArray<EmissionWidthRun> Widths { get; } = widths;

    public ReadOnlyMemory<byte> ToUnicode { get; } = toUnicode;

    public string BaseName { get; } = baseName;

    public int Flags { get; } = flags;

    public ImmutableArray<int> BoundingBox { get; } = boundingBox;

    public double ItalicAngle { get; } = italicAngle;

    public int Ascent { get; } = ascent;

    public int Descent { get; } = descent;

    public int CapHeight { get; } = capHeight;
}

internal sealed class EmissionFont(
    string key,
    string? base14,
    EmissionFontProgram? program,
    ReverseFont extraction)
{
    public string Key { get; } = key;

    public string? Base14 { get; } = base14;

    public string Base14Name => Base14 ?? "Helvetica";

    public EmissionFontProgram? Program { get; } = program;

    public bool IsEmbedded => Program is not null;

    public ReverseFont Extraction { get; } = extraction;
}

internal sealed class EmissionImage(
    object identity,
    string key,
    EmissionImagePayload image,
    EmissionImagePayload? softMask)
{
    public object Identity { get; } = identity;

    public string Key { get; } = key;

    public EmissionImagePayload Image { get; } = image;

    public EmissionImagePayload? SoftMask { get; } = softMask;
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

internal sealed class EmissionPattern(string key, GradientPaint gradient, Matrix matrix)
{
    public string Key { get; } = key;

    public GradientPaint Gradient { get; } = gradient;

    public Matrix Matrix { get; } = matrix;
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
    byte[] content,
    ImmutableArray<double> boundingBox,
    string? colorSpace,
    bool? isolated,
    bool? knockout,
    ImmutableArray<KeyValuePair<string, EmissionImagePayload>> xObjects)
{
    public ReadOnlyMemory<byte> Content { get; } = content;

    public ImmutableArray<double> BoundingBox { get; } = boundingBox;

    public string? ColorSpace { get; } = colorSpace;

    public bool? Isolated { get; } = isolated;

    public bool? Knockout { get; } = knockout;

    public ImmutableArray<KeyValuePair<string, EmissionImagePayload>> XObjects { get; } = xObjects;
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf.Output;

internal sealed class DocumentOutput(
    ImmutableArray<PageOutput> pages,
    StructureElementSnapshot? structure,
    ImmutableDictionary<string, OutputAnchor> anchors,
    ImmutableArray<string> unmappedRoles)
{
    public ImmutableArray<PageOutput> Pages { get; } = pages;

    public StructureElementSnapshot? Structure { get; } = structure;

    public ImmutableDictionary<string, OutputAnchor> Anchors { get; } = anchors;

    public ImmutableArray<string> UnmappedRoles { get; } = unmappedRoles;
}

internal sealed class PageOutput(
    byte[] content,
    ImmutableArray<OutputFont> fonts,
    ImmutableArray<OutputImage> images,
    ImmutableArray<OutputLink> links,
    ImmutableArray<OutputExtGState> extGStates,
    ImmutableArray<OutputPattern> patterns)
{
    public ReadOnlyMemory<byte> Content => ContentArray;

    public byte[] ContentArray { get; } = content;

    public ImmutableArray<OutputFont> Fonts { get; } = fonts;

    public ImmutableArray<OutputImage> Images { get; } = images;

    public ImmutableArray<OutputLink> Links { get; } = links;

    public ImmutableArray<OutputExtGState> ExtGStates { get; } = extGStates;

    public ImmutableArray<OutputPattern> Patterns { get; } = patterns;
}

internal enum OutputFontFileKind
{
    Glyf,
    Cff,
}

internal readonly record struct OutputWidthRun(int Cid, ImmutableArray<int> Widths);

internal sealed class OutputFontProgram(
    OutputFontFileKind kind,
    byte[] file,
    byte[] cidSet,
    ImmutableArray<OutputWidthRun> widths,
    byte[] toUnicode,
    string baseName,
    int flags,
    ImmutableArray<int> boundingBox,
    double italicAngle,
    int ascent,
    int descent,
    int capHeight)
{
    public OutputFontFileKind Kind { get; } = kind;

    public ReadOnlyMemory<byte> File { get; } = file;

    public ReadOnlyMemory<byte> CidSet { get; } = cidSet;

    public ImmutableArray<OutputWidthRun> Widths { get; } = widths;

    public ReadOnlyMemory<byte> ToUnicode { get; } = toUnicode;

    public string BaseName { get; } = baseName;

    public int Flags { get; } = flags;

    public ImmutableArray<int> BoundingBox { get; } = boundingBox;

    public double ItalicAngle { get; } = italicAngle;

    public int Ascent { get; } = ascent;

    public int Descent { get; } = descent;

    public int CapHeight { get; } = capHeight;
}

internal sealed class OutputFont(
    string key,
    string? base14,
    OutputFontProgram? program)
{
    public string Key { get; } = key;

    public string? Base14 { get; } = base14;

    public string Base14Name => Base14 ?? "Helvetica";

    public OutputFontProgram? Program { get; } = program;

    public bool IsEmbedded => Program is not null;
}

internal sealed class OutputImage(
    DecodedImage identity,
    string key,
    OutputImagePayload image,
    OutputImagePayload? softMask)
{
    public DecodedImage Identity { get; } = identity;

    public string Key { get; } = key;

    public OutputImagePayload Image { get; } = image;

    public OutputImagePayload? SoftMask { get; } = softMask;
}

internal readonly record struct OutputLink(
    double X1,
    double Y1,
    double X2,
    double Y2,
    string? Uri,
    string? Destination,
    int? StructureElementId);

internal readonly record struct OutputAnchor(PageOutput Page, double Top);

internal sealed class OutputExtGState(
    string key,
    double fillAlpha,
    double strokeAlpha,
    BlendMode? blend,
    OutputSoftMask? softMask)
{
    public string Key { get; } = key;

    public double FillAlpha { get; } = fillAlpha;

    public double StrokeAlpha { get; } = strokeAlpha;

    public BlendMode? Blend { get; } = blend;

    public OutputSoftMask? SoftMask { get; } = softMask;

}

internal sealed class OutputPattern(string key, GradientPaint gradient, Matrix matrix)
{
    public string Key { get; } = key;

    public GradientPaint Gradient { get; } = gradient;

    public Matrix Matrix { get; } = matrix;
}

internal sealed class OutputSoftMask(
    OutputTransparencyGroup group)
{
    public OutputTransparencyGroup Group { get; } = group;
}

internal sealed class OutputTransparencyGroup(
    byte[] content,
    ImmutableArray<double> boundingBox,
    string? colorSpace,
    ImmutableArray<KeyValuePair<string, OutputImagePayload>> xObjects)
{
    public ReadOnlyMemory<byte> Content { get; } = content;

    public ImmutableArray<double> BoundingBox { get; } = boundingBox;

    public string? ColorSpace { get; } = colorSpace;

    public ImmutableArray<KeyValuePair<string, OutputImagePayload>> XObjects { get; } = xObjects;
}

internal readonly record struct StructureKidSnapshot(
    StructureElementSnapshot? Child,
    PageOutput? Page,
    int Mcid);

internal sealed class StructureElementSnapshot(
    int id,
    string type,
    string? alt,
    string? actualText,
    string? language,
    SemanticHeaderScope headerScope,
    int rowSpan,
    int columnSpan,
    ImmutableArray<StructureKidSnapshot> kids)
{
    public int Id { get; } = id;

    public string Type { get; } = type;

    public string? Alt { get; } = alt;

    public string? ActualText { get; } = actualText;

    public string? Language { get; } = language;

    public SemanticHeaderScope HeaderScope { get; } = headerScope;

    public int RowSpan { get; } = rowSpan;

    public int ColumnSpan { get; } = columnSpan;

    public ImmutableArray<StructureKidSnapshot> Kids { get; } = kids;
}

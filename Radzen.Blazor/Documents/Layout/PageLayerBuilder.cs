using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class PageLayerBuilder(LayoutCaptureContext capture)
{
    public List<LaidOutLine> Lines { get; } = [];

    public List<LaidOutImage> Images { get; } = [];

    public List<LaidOutCodeSymbol> CodeSymbols { get; } = [];

    public List<LaidOutTableFragment> Tables { get; } = [];

    public List<LaidOutBox> Boxes { get; } = [];

    public bool HasContent
        => Lines.Count > 0 || Images.Count > 0 || CodeSymbols.Count > 0 || Tables.Count > 0 || Boxes.Count > 0;

    public LaidOutLayer Build() => new()
    {
        Id = capture.Node(),
        Lines = [.. Lines],
        Images = [.. Images],
        CodeSymbols = [.. CodeSymbols],
        Tables = [.. Tables],
        Boxes = [.. Boxes],
    };
}

using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal sealed class PageLayerBuilder
{
    public List<PositionedLine> Lines { get; } = [];

    public List<PositionedImage> Images { get; } = [];

    public List<PositionedCodeSymbol> CodeSymbols { get; } = [];

    public List<PositionedTableFragment> Tables { get; } = [];

    public List<PositionedBox> Boxes { get; } = [];

    public bool HasContent
        => Lines.Count > 0 || Images.Count > 0 || CodeSymbols.Count > 0 || Tables.Count > 0 || Boxes.Count > 0;

    public PageLayer Build() => new()
    {
        Lines = [.. Lines],
        Images = [.. Images],
        CodeSymbols = [.. CodeSymbols],
        Tables = [.. Tables],
        Boxes = [.. Boxes],
    };
}

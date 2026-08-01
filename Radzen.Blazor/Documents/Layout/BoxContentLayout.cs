using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal abstract class CellItem
{
    public required double Height { get; init; }

    public void Place(CellPlacement placement)
    {
        if (Marker is { } marker)
        {
            placement.Add(new LaidOutLine
            {
                Line = marker.Line,
                Source = placement.Capture.Source(marker.Source),
                X = placement.ContentBox.Left,
                Y = placement.CursorY,
                ZOrder = placement.NextOrder(),
            });
        }

        PlaceContent(placement);
    }

    protected virtual (LineBox Line, Block Source)? Marker => null;

    protected abstract void PlaceContent(CellPlacement placement);
}

internal abstract class BlockCellItem<TBlock> : CellItem
    where TBlock : Block
{
    public required TBlock Block { get; init; }

    public LineBox? MarkerLine { get; init; }

    protected sealed override (LineBox Line, Block Source)? Marker
        => MarkerLine is { } line ? (line, Block) : null;
}

internal sealed class SpacingCellItem : CellItem
{
    protected override void PlaceContent(CellPlacement placement)
    {
    }
}

internal sealed class LineCellItem : CellItem
{
    public required LineBox Line { get; init; }

    public required Block Source { get; init; }

    protected override void PlaceContent(CellPlacement placement)
        => placement.Add(new LaidOutLine
        {
            Line = Line,
            Source = placement.Capture.Source(Source),
            X = placement.ContentBox.Left,
            Y = placement.CursorY,
            ZOrder = placement.NextOrder(),
        });
}

internal sealed class TableCellItem : BlockCellItem<Table>
{
    public required LaidOutTable Layout { get; init; }

    protected override void PlaceContent(CellPlacement placement)
        => placement.Add(new LaidOutTablePlacement
        {
            Layout = Layout,
            X = placement.ContentBox.Left,
            Y = placement.CursorY,
            ZOrder = placement.NextOrder(),
        });
}

internal sealed class ImageCellItem : BlockCellItem<Image>
{
    public required double Indent { get; init; }

    public required double Width { get; init; }

    protected override void PlaceContent(CellPlacement placement)
    {
        var capture = placement.Capture;
        var contentBox = placement.ContentBox;
        var availableWidth = Math.Max(0, contentBox.Width - Indent);
        placement.Add(new LaidOutImage
        {
            Source = capture.Source(Block),
            Paint = GeometryCapture.Image(Block, capture),
            X = contentBox.Left + Indent
                + HorizontalAlignmentOffset.Of(placement.Effective(Block.Alignment), availableWidth, Width),
            Y = placement.CursorY,
            Width = Width,
            Height = Height,
            ZOrder = placement.NextOrder(),
        });
    }
}

internal sealed class CodeSymbolCellItem : BlockCellItem<Block>
{
    public required double Indent { get; init; }

    public required double Width { get; init; }

    public ImmutableArray<LaidOutCaptionLine>? Caption { get; init; }

    protected override void PlaceContent(CellPlacement placement)
    {
        var contentBox = placement.ContentBox;
        var availableWidth = Math.Max(0, contentBox.Width - Indent);
        placement.Add(new LaidOutCodeSymbol
        {
            Source = placement.Capture.Source(Block),
            Modules = CodeSymbolDispatch.Modules(Block),
            Width = Width,
            Height = Height,
            Caption = Caption,
            X = contentBox.Left + Indent
                + HorizontalAlignmentOffset.Of(
                    placement.Effective(CodeSymbolDispatch.Alignment(Block)),
                    availableWidth,
                    Width),
            Y = placement.CursorY,
            ZOrder = placement.NextOrder(),
        });
    }
}

internal sealed class BoxCellItem : BlockCellItem<Container>
{
    public required BoxContentLayout.Measured Content { get; init; }

    public required double Opacity { get; init; }

    public required double Indent { get; init; }

    public required double Width { get; init; }

    protected override void PlaceContent(CellPlacement placement)
    {
        var capture = placement.Capture;
        var contentBox = placement.ContentBox;
        var padding = Block.EffectivePadding;
        var availableWidth = Math.Max(0, contentBox.Width - Indent);
        var indent = Indent + Math.Max(0, HorizontalAlignmentOffset.Of(Block.Alignment, availableWidth, Width));
        var innerBox = new Rect(padding.Left, padding.Top, Math.Max(0, Width - padding.Horizontal), Content.Height);
        placement.Add(new LaidOutBox
        {
            Id = capture.Node(),
            Source = capture.Source(Block),
            Content = BoxContentLayout.Position(Content, innerBox, HorizontalAlignment.Left, VerticalAlignment.Top),
            Bounds = new Rect(contentBox.Left + indent, placement.CursorY, Width, Height),
            Style = GeometryCapture.Box(Block, Width, Height, capture),
            Padding = padding,
            Opacity = Opacity,
            ZOrder = placement.NextOrder(),
        });
    }
}

internal sealed class CellPlacement(LayoutCaptureContext capture, Rect contentBox, HorizontalAlignment align)
{
    private readonly List<LaidOutLine> lines = [];
    private readonly List<LaidOutImage> images = [];
    private readonly List<LaidOutCodeSymbol> codeSymbols = [];
    private readonly List<LaidOutTablePlacement> tables = [];
    private readonly List<LaidOutBox> boxes = [];
    private int order;

    public LayoutCaptureContext Capture => capture;

    public Rect ContentBox => contentBox;

    public double CursorY { get; set; }

    public int NextOrder() => order++;

    public HorizontalAlignment Effective(HorizontalAlignment blockAlignment)
        => blockAlignment == HorizontalAlignment.Left ? align : blockAlignment;

    public void Add(LaidOutLine line) => lines.Add(line);

    public void Add(LaidOutImage image) => images.Add(image);

    public void Add(LaidOutCodeSymbol codeSymbol) => codeSymbols.Add(codeSymbol);

    public void Add(LaidOutTablePlacement table) => tables.Add(table);

    public void Add(LaidOutBox box) => boxes.Add(box);

    public LaidOutBoxContent Build(double height) => new()
    {
        Height = height,
        Lines = [.. lines],
        Images = [.. images],
        CodeSymbols = [.. codeSymbols],
        Tables = [.. tables],
        Boxes = [.. boxes],
    };
}

internal static class BoxContentLayout
{
    internal readonly struct Measured
    {
        public required List<CellItem> Items { get; init; }

        public required double Height { get; init; }

        public required LayoutCaptureContext Capture { get; init; }
    }

    public static LaidOutBoxContent Layout(
        BlockCollection blocks,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture)
        => Position(
            Measure(blocks, contentBox.Width, align, fonts, resolution, capture),
            contentBox,
            align,
            vAlign);

    public static Measured Measure(
        BlockCollection blocks,
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture)
    {
        var engine = FlowPlacementEngine.ForBox(
            contentWidth,
            align,
            fonts,
            resolution,
            capture);
        foreach (var block in BlockExpander.ExpandBlocks(blocks, contentWidth, resolution))
        {
            engine.Place(block, 0);
        }

        return new Measured
        {
            Items = engine.Items,
            Height = engine.Cursor,
            Capture = capture,
        };
    }

    public static LaidOutBoxContent Position(
        in Measured measured,
        Rect contentBox,
        HorizontalAlignment align,
        VerticalAlignment vAlign)
    {
        var factor = vAlign switch
        {
            VerticalAlignment.Middle => 0.5,
            VerticalAlignment.Bottom => 1.0,
            _ => 0.0,
        };
        var offset = (contentBox.Height - measured.Height) * factor;

        var placement = new CellPlacement(measured.Capture, contentBox, align)
        {
            CursorY = contentBox.Top + offset,
        };

        foreach (var item in measured.Items)
        {
            item.Place(placement);
            placement.CursorY += item.Height;
        }

        return placement.Build(measured.Height);
    }
}

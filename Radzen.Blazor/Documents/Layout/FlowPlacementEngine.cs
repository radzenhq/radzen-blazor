using System;
using System.Collections.Generic;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal enum FlowBreakability
{
    Continuous,
    Paginated,
}

internal readonly record struct FlowPlacementPolicy(
    FlowBreakability Breakability,
    double AvailableHeight,
    Action Continue,
    bool GuardParagraphMarkers);

internal abstract class FlowPlacementEngine(FlowPlacementPolicy policy) : ILoweredBlockHandler<int, Nothing>
{
    protected FlowPlacementPolicy Policy { get; } = policy;

    internal static PagesFlowPlacementEngine ForPages(PaginationContext pages)
        => new(
            new FlowPlacementPolicy(
                FlowBreakability.Paginated,
                pages.AvailableHeight,
                pages.PlaceBreak,
                GuardParagraphMarkers: true),
            pages);

    internal static BandFlowPlacementEngine ForBand(
        BandLayout band,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        => new(
            new FlowPlacementPolicy(
                FlowBreakability.Continuous,
                double.PositiveInfinity,
                static () => { },
                GuardParagraphMarkers: false),
            band,
            width,
            fonts,
            measureImage,
            resolution,
            capture);

    internal static BoxFlowPlacementEngine ForBox(
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        => new(
            new FlowPlacementPolicy(
                FlowBreakability.Continuous,
                double.PositiveInfinity,
                static () => { },
                GuardParagraphMarkers: true),
            contentWidth,
            align,
            fonts,
            measureImage,
            resolution,
            capture);

    internal void Place(Block block, int index)
        => LoweredBlockDispatch.Place(block, this, index);

    public Nothing PageBreak(PageBreak block, int index)
    {
        if (Policy.Breakability == FlowBreakability.Paginated)
        {
            Policy.Continue();
        }

        return default;
    }

    public abstract Nothing Table(Table table, int index);

    public abstract Nothing Container(Container container, int index);

    public abstract Nothing Image(Image image, int index);

    public abstract Nothing CodeSymbol(Block block, int index);

    public abstract Nothing Paragraph(Paragraph paragraph, int index);
}

internal sealed class PagesFlowPlacementEngine(FlowPlacementPolicy policy, PaginationContext pages)
    : FlowPlacementEngine(policy)
{
    public override Nothing Table(Table table, int index)
    {
        pages.PlaceTable(index, table);
        return default;
    }

    public override Nothing Container(Container container, int index)
    {
        if (OverlayBoxPlacer.IsSpecial(container))
        {
            pages.PlaceSpecialContainer(container);
        }
        else
        {
            pages.PlaceBox(index, container);
        }

        return default;
    }

    public override Nothing Image(Image image, int index)
    {
        pages.PlaceImage(image);
        return default;
    }

    public override Nothing CodeSymbol(Block block, int index)
    {
        pages.PlaceCodeSymbol(block);
        return default;
    }

    public override Nothing Paragraph(Paragraph paragraph, int index)
    {
        pages.PlaceParagraph(index, paragraph);
        return default;
    }
}

internal abstract class ContentFlowPlacementEngine(
    FlowPlacementPolicy policy,
    double width,
    HorizontalAlignment? align,
    FontCollection fonts,
    Func<Image, double, (double Width, double Height)>? measureImage,
    LoweringContext resolution,
    LayoutCaptureContext capture)
    : FlowPlacementEngine(policy)
{
    protected FontCollection Fonts => fonts;

    protected LoweringContext Resolution => resolution;

    protected LayoutCaptureContext Capture => capture;

    protected Func<Image, double, (double Width, double Height)>? ImageMeasure => measureImage;

    internal double Cursor { get; private protected set; }

    protected LineBox? Marker(Block block)
    {
        if (Policy.GuardParagraphMarkers && block is Paragraph)
        {
            return null;
        }

        return resolution.ListMarker(block) is { } marker
            ? LineLayouter.MarkerLine(marker, fonts, capture)
            : null;
    }

    public sealed override Nothing Table(Table table, int index)
    {
        var layout = LoweredBlockDispatch.LayoutTable(
            table,
            width,
            fonts,
            measureImage,
            resolution,
            capture);
        PlaceTable(table, layout);
        return default;
    }

    public sealed override Nothing Container(Container container, int index)
    {
        var indent = resolution.BlockIndent(container);
        PlaceContainer(container, indent, Math.Max(0, width - indent));
        return default;
    }

    public sealed override Nothing Image(Image image, int index)
    {
        var indent = resolution.BlockIndent(image);
        var availableWidth = Math.Max(0, width - indent);
        var (imageWidth, imageHeight) = FlowContentPlacer.MeasureImage(
            image,
            availableWidth,
            measureImage);
        PlaceImage(image, indent, availableWidth, imageWidth, imageHeight);
        return default;
    }

    public sealed override Nothing CodeSymbol(Block block, int index)
    {
        var indent = resolution.BlockIndent(block);
        var availableWidth = Math.Max(0, width - indent);
        var (symbolWidth, symbolHeight) = CodeSymbolDispatch.Measure(
            block,
            capture,
            fonts,
            resolution);
        PlaceCodeSymbol(block, indent, availableWidth, symbolWidth, symbolHeight);
        return default;
    }

    public sealed override Nothing Paragraph(Paragraph paragraph, int index)
    {
        var lines = LineLayouter.Layout(
            paragraph,
            width,
            fonts,
            capture,
            resolution.Alignment(paragraph) ?? align,
            resolution);
        PlaceParagraph(paragraph, lines, resolution.Format(paragraph));
        return default;
    }

    protected abstract void PlaceTable(Table table, LaidOutTable layout);

    protected abstract void PlaceContainer(Container container, double indent, double availableWidth);

    protected abstract void PlaceImage(
        Image image,
        double indent,
        double availableWidth,
        double imageWidth,
        double imageHeight);

    protected abstract void PlaceCodeSymbol(
        Block block,
        double indent,
        double availableWidth,
        double symbolWidth,
        double symbolHeight);

    protected abstract void PlaceParagraph(
        Paragraph paragraph,
        IReadOnlyList<LineBox> lines,
        ResolvedParagraphFormat format);
}

internal sealed class BandFlowPlacementEngine(
    FlowPlacementPolicy policy,
    BandLayout band,
    double width,
    FontCollection fonts,
    Func<Image, double, (double Width, double Height)>? measureImage,
    LoweringContext resolution,
    LayoutCaptureContext capture)
    : ContentFlowPlacementEngine(policy, width, align: null, fonts, measureImage, resolution, capture)
{
    private int order;

    protected override void PlaceTable(Table table, LaidOutTable layout)
    {
        AddMarker(table);
        var tableOrder = order++;
        foreach (var fragment in TablePaginator.Paginate(
            layout,
            table,
            Policy.AvailableHeight,
            Capture))
        {
            band.Content.Tables.Add(FlowContentPlacer.Table(
                table,
                layout,
                fragment,
                Cursor,
                tableOrder,
                Capture));
            Cursor += fragment.Height;
        }
    }

    protected override void PlaceContainer(Container container, double indent, double availableWidth)
    {
        var measured = OverlayBoxPlacer.MeasureBox(
            container,
            availableWidth,
            Fonts,
            ImageMeasure,
            Resolution,
            Capture);
        AddMarker(container);
        var box = OverlayBoxPlacer.BuildBox(
            container,
            measured,
            availableWidth,
            Cursor,
            order++,
            transform: null,
            Resolution,
            indent);
        band.Content.Boxes.Add(box);
        Cursor += box.Bounds.Height;
    }

    protected override void PlaceImage(
        Image image,
        double indent,
        double availableWidth,
        double imageWidth,
        double imageHeight)
    {
        AddMarker(image);
        band.Content.Images.Add(FlowContentPlacer.Image(
            image,
            availableWidth,
            Cursor,
            imageWidth,
            imageHeight,
            Capture,
            order++,
            indent));
        Cursor += imageHeight;
    }

    protected override void PlaceCodeSymbol(
        Block block,
        double indent,
        double availableWidth,
        double symbolWidth,
        double symbolHeight)
    {
        AddMarker(block);
        band.Content.CodeSymbols.Add(FlowContentPlacer.CodeSymbol(
            block,
            availableWidth,
            Cursor,
            symbolWidth,
            symbolHeight,
            Fonts,
            Resolution,
            Capture,
            order++,
            indent));
        Cursor += symbolHeight;
    }

    protected override void PlaceParagraph(
        Paragraph paragraph,
        IReadOnlyList<LineBox> lines,
        ResolvedParagraphFormat format)
    {
        var y = Cursor + format.SpacingBefore.Point;
        foreach (var line in lines)
        {
            band.Content.Lines.Add(new LaidOutLine
            {
                Line = line,
                Source = Capture.Source(paragraph),
                X = 0,
                Y = y,
                ZOrder = order++,
            });
            y += line.Height;
        }

        Cursor = y + format.SpacingAfter.Point;
    }

    private void AddMarker(Block block)
    {
        if (Marker(block) is not { } marker)
        {
            return;
        }

        band.Content.Lines.Add(new LaidOutLine
        {
            Line = marker,
            Source = Capture.Source(block),
            X = 0,
            Y = Cursor,
            ZOrder = order++,
        });
    }
}

internal sealed class BoxFlowPlacementEngine(
    FlowPlacementPolicy policy,
    double width,
    HorizontalAlignment? align,
    FontCollection fonts,
    Func<Image, double, (double Width, double Height)>? measureImage,
    LoweringContext resolution,
    LayoutCaptureContext capture)
    : ContentFlowPlacementEngine(policy, width, align, fonts, measureImage, resolution, capture)
{
    internal List<CellItem> Items { get; } = [];

    protected override void PlaceTable(Table table, LaidOutTable layout)
    {
        Items.Add(new TableCellItem
        {
            MarkerLine = Marker(table),
            Block = table,
            Layout = layout,
            Height = layout.Height,
        });
        Cursor += layout.Height;
    }

    protected override void PlaceContainer(Container container, double indent, double availableWidth)
    {
        var padding = container.EffectivePadding;
        var boxWidth = container.Width?.Point ?? availableWidth;
        var inner = BoxContentLayout.Measure(
            container.Blocks,
            Math.Max(0, boxWidth - padding.Horizontal),
            null,
            Fonts,
            ImageMeasure,
            Resolution,
            Capture);
        var boxHeight = inner.Height + padding.Vertical;
        Items.Add(new BoxCellItem
        {
            Block = container,
            MarkerLine = Marker(container),
            Content = inner,
            Opacity = Resolution.Opacities.ContainerOpacity(container),
            Indent = indent,
            Width = boxWidth,
            Height = boxHeight,
        });
        Cursor += boxHeight;
    }

    protected override void PlaceImage(
        Image image,
        double indent,
        double availableWidth,
        double imageWidth,
        double imageHeight)
    {
        Items.Add(new ImageCellItem
        {
            MarkerLine = Marker(image),
            Block = image,
            Indent = indent,
            Width = imageWidth,
            Height = imageHeight,
        });
        Cursor += imageHeight;
    }

    protected override void PlaceCodeSymbol(
        Block block,
        double indent,
        double availableWidth,
        double symbolWidth,
        double symbolHeight)
    {
        Items.Add(new CodeSymbolCellItem
        {
            Block = block,
            MarkerLine = Marker(block),
            Caption = CodeSymbolDispatch.Caption(block, Fonts, Resolution, Capture),
            Indent = indent,
            Width = symbolWidth,
            Height = symbolHeight,
        });
        Cursor += symbolHeight;
    }

    protected override void PlaceParagraph(
        Paragraph paragraph,
        IReadOnlyList<LineBox> lines,
        ResolvedParagraphFormat format)
    {
        var spacingBefore = format.SpacingBefore.Point;
        if (spacingBefore > 0)
        {
            Items.Add(new SpacingCellItem { Height = spacingBefore });
            Cursor += spacingBefore;
        }

        foreach (var line in lines)
        {
            Items.Add(new LineCellItem
            {
                Line = line,
                Source = paragraph,
                Height = line.Height,
            });
            Cursor += line.Height;
        }

        var spacingAfter = format.SpacingAfter.Point;
        if (spacingAfter > 0)
        {
            Items.Add(new SpacingCellItem { Height = spacingAfter });
            Cursor += spacingAfter;
        }
    }
}

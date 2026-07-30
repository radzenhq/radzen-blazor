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

internal sealed class FlowPlacementEngine : ILoweredBlockHandler<int, Nothing>
{
    private enum Target
    {
        Pages,
        Band,
        Box,
    }

    private readonly Target target;
    private readonly FlowPlacementPolicy policy;
    private readonly PaginationContext? pages;
    private readonly BandLayout? band;
    private readonly double width;
    private readonly HorizontalAlignment? align;
    private readonly FontCollection fonts;
    private readonly Func<Image, double, (double Width, double Height)>? measureImage;
    private readonly LoweringContext resolution;
    private readonly LayoutCaptureContext capture;
    private int order;

    private FlowPlacementEngine(
        Target target,
        FlowPlacementPolicy policy,
        PaginationContext? pages,
        BandLayout? band,
        double width,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        this.target = target;
        this.policy = policy;
        this.pages = pages;
        this.band = band;
        this.width = width;
        this.align = align;
        this.fonts = fonts;
        this.measureImage = measureImage;
        this.resolution = resolution;
        this.capture = capture;
    }

    internal static FlowPlacementEngine ForPages(PaginationContext pages)
        => new(
            Target.Pages,
            new FlowPlacementPolicy(
                FlowBreakability.Paginated,
                pages.AvailableHeight,
                pages.PlaceBreak,
                GuardParagraphMarkers: true),
            pages,
            null,
            0,
            null,
            pages.Fonts,
            pages.MeasureImage,
            pages.Resolution,
            pages.Capture);

    internal static FlowPlacementEngine ForBand(
        BandLayout band,
        double width,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        => new(
            Target.Band,
            new FlowPlacementPolicy(
                FlowBreakability.Continuous,
                double.PositiveInfinity,
                static () => { },
                GuardParagraphMarkers: false),
            null,
            band,
            width,
            null,
            fonts,
            measureImage,
            resolution,
            capture);

    internal static FlowPlacementEngine ForBox(
        double contentWidth,
        HorizontalAlignment? align,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage,
        LoweringContext resolution,
        LayoutCaptureContext capture)
        => new(
            Target.Box,
            new FlowPlacementPolicy(
                FlowBreakability.Continuous,
                double.PositiveInfinity,
                static () => { },
                GuardParagraphMarkers: true),
            null,
            null,
            contentWidth,
            align,
            fonts,
            measureImage,
            resolution,
            capture);

    internal List<BoxContentLayout.CellItem> Items { get; } = [];

    internal double Cursor { get; private set; }

    internal void Place(Block block, int index)
        => LoweredBlockDispatch.Place(block, this, index);

    private LineBox? Marker(Block block)
    {
        if (policy.GuardParagraphMarkers && block is Paragraph)
        {
            return null;
        }

        return resolution.ListMarker(block) is { } marker
            ? LineLayouter.MarkerLine(marker, fonts, capture)
            : null;
    }

    private void AddBandMarker(Block block)
    {
        if (Marker(block) is not { } marker)
        {
            return;
        }

        band!.Content.Lines.Add(new LaidOutLine
        {
            Line = marker,
            Source = capture.Source(block),
            X = 0,
            Y = Cursor,
            ZOrder = order++,
        });
    }

    public Nothing PageBreak(PageBreak block, int index)
    {
        if (policy.Breakability == FlowBreakability.Paginated)
        {
            policy.Continue();
        }

        return default;
    }

    public Nothing Table(Table table, int index)
    {
        if (target == Target.Pages)
        {
            pages!.PlaceTable(index, table);
            return default;
        }

        var layout = LoweredBlockDispatch.LayoutTable(
            table,
            width,
            fonts,
            measureImage,
            resolution,
            capture);
        if (target == Target.Box)
        {
            Items.Add(new BoxContentLayout.CellItem
            {
                MarkerLine = Marker(table),
                MarkerSource = table,
                Table = layout,
                Height = layout.Height,
            });
            Cursor += layout.Height;
            return default;
        }

        AddBandMarker(table);
        var tableOrder = order++;
        foreach (var fragment in TablePaginator.Paginate(
            layout,
            table,
            policy.AvailableHeight,
            capture))
        {
            band!.Content.Tables.Add(FlowContentPlacer.Table(
                table,
                layout,
                fragment,
                Cursor,
                tableOrder,
                capture));
            Cursor += fragment.Height;
        }

        return default;
    }

    public Nothing Container(Container container, int index)
    {
        if (target == Target.Pages)
        {
            if (OverlayBoxPlacer.IsSpecial(container))
            {
                pages!.PlaceSpecialContainer(container);
            }
            else
            {
                pages!.PlaceBox(index, container);
            }

            return default;
        }

        var indent = resolution.BlockIndent(container);
        var availableWidth = Math.Max(0, width - indent);
        if (target == Target.Box)
        {
            var padding = container.Padding.Point;
            var boxWidth = container.Width?.Point ?? availableWidth;
            var inner = BoxContentLayout.Measure(
                container.Blocks,
                Math.Max(0, boxWidth - (2 * padding)),
                null,
                fonts,
                measureImage,
                resolution,
                capture);
            var boxHeight = inner.Height + (2 * padding);
            Items.Add(new BoxContentLayout.CellItem
            {
                Box = container,
                MarkerLine = Marker(container),
                MarkerSource = container,
                BoxContent = inner,
                BoxOpacity = resolution.Opacities.ContainerOpacity(container),
                Indent = indent,
                Width = boxWidth,
                Height = boxHeight,
            });
            Cursor += boxHeight;
            return default;
        }

        var measured = OverlayBoxPlacer.MeasureBox(
            container,
            availableWidth,
            fonts,
            measureImage,
            resolution,
            capture);
        AddBandMarker(container);
        var box = OverlayBoxPlacer.BuildBox(
            container,
            measured,
            availableWidth,
            Cursor,
            order++,
            transform: null,
            resolution,
            indent);
        band!.Content.Boxes.Add(box);
        Cursor += box.Bounds.Height;
        return default;
    }

    public Nothing Image(Image image, int index)
    {
        if (target == Target.Pages)
        {
            pages!.PlaceImage(image);
            return default;
        }

        var indent = resolution.BlockIndent(image);
        var availableWidth = Math.Max(0, width - indent);
        var (imageWidth, imageHeight) = FlowContentPlacer.MeasureImage(
            image,
            availableWidth,
            measureImage);
        if (target == Target.Box)
        {
            Items.Add(new BoxContentLayout.CellItem
            {
                MarkerLine = Marker(image),
                MarkerSource = image,
                Image = image,
                Indent = indent,
                Width = imageWidth,
                Height = imageHeight,
            });
            Cursor += imageHeight;
            return default;
        }

        AddBandMarker(image);
        band!.Content.Images.Add(FlowContentPlacer.Image(
            image,
            availableWidth,
            Cursor,
            imageWidth,
            imageHeight,
            capture,
            order++,
            indent));
        Cursor += imageHeight;
        return default;
    }

    public Nothing CodeSymbol(Block block, int index)
    {
        if (target == Target.Pages)
        {
            pages!.PlaceCodeSymbol(block);
            return default;
        }

        var indent = resolution.BlockIndent(block);
        var availableWidth = Math.Max(0, width - indent);
        var (codeSymbolWidth, codeSymbolHeight) = CodeSymbolDispatch.Measure(
            block,
            capture,
            fonts,
            resolution);
        if (target == Target.Box)
        {
            Items.Add(new BoxContentLayout.CellItem
            {
                CodeSymbol = block,
                MarkerLine = Marker(block),
                MarkerSource = block,
                Caption = CodeSymbolDispatch.Caption(block, fonts, resolution, capture),
                Indent = indent,
                Width = codeSymbolWidth,
                Height = codeSymbolHeight,
            });
            Cursor += codeSymbolHeight;
            return default;
        }

        AddBandMarker(block);
        band!.Content.CodeSymbols.Add(FlowContentPlacer.CodeSymbol(
            block,
            availableWidth,
            Cursor,
            codeSymbolWidth,
            codeSymbolHeight,
            fonts,
            resolution,
            capture,
            order++,
            indent));
        Cursor += codeSymbolHeight;
        return default;
    }

    public Nothing Paragraph(Paragraph paragraph, int index)
    {
        if (target == Target.Pages)
        {
            pages!.PlaceParagraph(index, paragraph);
            return default;
        }

        var lines = LineLayouter.Layout(
            paragraph,
            width,
            fonts,
            capture,
            resolution.Alignment(paragraph) ?? align,
            resolution);
        var format = resolution.Format(paragraph);
        if (target == Target.Box)
        {
            var spacingBefore = format.SpacingBefore.Point;
            if (spacingBefore > 0)
            {
                Items.Add(new BoxContentLayout.CellItem { Height = spacingBefore });
                Cursor += spacingBefore;
            }

            foreach (var line in lines)
            {
                Items.Add(new BoxContentLayout.CellItem
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
                Items.Add(new BoxContentLayout.CellItem { Height = spacingAfter });
                Cursor += spacingAfter;
            }

            return default;
        }

        var y = Cursor + format.SpacingBefore.Point;
        foreach (var line in lines)
        {
            band!.Content.Lines.Add(new LaidOutLine
            {
                Line = line,
                Source = capture.Source(paragraph),
                X = 0,
                Y = y,
                ZOrder = order++,
            });
            y += line.Height;
        }

        Cursor = y + format.SpacingAfter.Point;
        return default;
    }
}

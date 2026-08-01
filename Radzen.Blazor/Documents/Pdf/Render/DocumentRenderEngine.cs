using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class DocumentRenderEngine
{
    private readonly RenderRequest request;
    private readonly LaidOutDocument laidOut;

    private readonly StructureTreeBuilder structureTree;
    private readonly FontRegistry fontRegistry;
    private readonly ImageRegistry imageRegistry;
    private readonly TextLineRecorder textRecorder;
    private readonly TableRecorder tableRecorder;
    private readonly BoxRecorder boxRecorder;
    private readonly ImageRecorder imageRecorder;
    private readonly CodeSymbolRecorder codeSymbolRecorder;
    private readonly WatermarkRecorder watermarkRecorder;

    private readonly bool markArtifacts;

    private DocumentRenderEngine(RenderRequest request, LaidOutDocument laidOut)
    {
        this.request = request;
        this.laidOut = laidOut;
        markArtifacts = request.Accessibility != PdfUaConformance.None
            || request.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

        structureTree = new(laidOut.Semantics, request);
        structureTree.Build();

        fontRegistry = new();
        imageRegistry = new(request.Decoders);
        textRecorder = new(
            fontRegistry,
            imageRegistry,
            structureTree,
            request.AllowUnsupportedCharacters);
        codeSymbolRecorder = new(structureTree);
        imageRecorder = new(imageRegistry, structureTree);
        tableRecorder = new(structureTree);
        boxRecorder = new(structureTree);
        watermarkRecorder = new(
            fontRegistry,
            imageRegistry,
            request.AllowUnsupportedCharacters);
    }

    internal static PortableDocument Generate(RenderRequest request, LaidOutDocument laidOut)
        => new DocumentRenderEngine(request, laidOut).Run();

    private PortableDocument CreateOutput()
    {
        var output = new PortableDocument
        {
            Conformance = request.Conformance,
            Accessibility = request.Accessibility,
            RoleMap = request.RoleMap,
            Encryption = request.Encryption,
            CompressOutput = request.CompressOutput,
            IncludeDocumentId = request.IncludeDocumentId,
            ViewerPreferences = request.ViewerPreferences,
            ImageDecoders = request.Decoders,
        };

        output.Info.Producer = request.Producer;

        foreach (var attachment in request.Attachments)
        {
            output.Attachments.Add(attachment);
        }

        foreach (var item in request.Outline)
        {
            output.Outline.Add(item);
        }

        foreach (var label in request.PageLabels)
        {
            output.PageLabels.Add(label);
        }

        foreach (var field in request.FormFields)
        {
            output.FormFields.Add(field);
        }

        return output;
    }

    private PortableDocument Run()
    {
        FontEmbedding.Ensure(laidOut.Fonts, request.AllowRestrictedEmbedding, request.AllowDegradedFonts);

        var portable = CreateOutput();
        portable.FontSnapshot = laidOut.Fonts;
        portable.Language = laidOut.Semantics.Language;
        PdfModelMapper.Apply(laidOut.Info, portable.Info);

        var paginated = laidOut.Pages;

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Length; i++)
        {
            plans.Add(GeneratePage(paginated[i]));
        }

        var fontPlans = fontRegistry.Plan();
        var pages = ImmutableArray.CreateBuilder<PageOutput>(plans.Count);
        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            using var finalizer = new PageWriter(
                structureTree,
                markArtifacts,
                plans[pageIndex],
                pageIndex,
                fontPlans);
            pages.Add(finalizer.Finalize());
        }

        var pageOutputs = pages.MoveToImmutable();
        var anchors = ImmutableDictionary.CreateBuilder<string, OutputAnchor>(System.StringComparer.Ordinal);
        for (var pageIndex = 0; pageIndex < paginated.Length; pageIndex++)
        {
            var pageHeight = paginated[pageIndex].Size.Height.Point;
            foreach (var anchor in paginated[pageIndex].Anchors)
            {
                if (!anchors.ContainsKey(anchor.Name))
                {
                    anchors.Add(
                        anchor.Name,
                        new OutputAnchor(pageOutputs[pageIndex], BottomUpSpace.FromTop(pageHeight, anchor.Top)));
                }
            }
        }

        portable.Output = new DocumentOutput(
            pageOutputs,
            structureTree.Capture(pageOutputs),
            anchors.ToImmutable(),
            [.. request.RoleMap.Entries],
            structureTree.UnmappedRoles);

        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            var pageOutput = pageOutputs[pageIndex];
            var page = new Page(plans[pageIndex].Size.Width, plans[pageIndex].Size.Height)
            {
                OutputIdentity = pageOutput,
            };
            page.SetLoadedContent(pageOutput.ContentArray);
            page.SetTextFonts(fontRegistry.ExtractionFonts(pageOutput));
            portable.Pages.Insert(portable.Pages.Count, page);
        }

        return portable;
    }

    private PagePlan GeneratePage(LaidOutPage page)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var context = new PageRenderContext(
            plan,
            textRecorder,
            codeSymbolRecorder,
            imageRecorder,
            tableRecorder,
            boxRecorder);
        var left = page.ContentBox.X;
        var contentTop = BottomUpSpace.FromTop(height, page.ContentBox.Y);

        foreach (var kind in PdfPaintOrder.Layers)
        {
            var (layer, top) = kind switch
            {
                PageLayerKind.Body => (page.Body, contentTop),
                PageLayerKind.Header => (page.HeaderLayer, BottomUpSpace.FromTop(height, page.HeaderTop)),
                _ => (page.FooterLayer, BottomUpSpace.FromTop(height, page.FooterTop)),
            };

            EmitLayer(context, kind, layer, left, top);
        }

        foreach (var link in page.Links)
        {
            plan.Links.Add(new OutputLink(
                link.Left,
                BottomUpSpace.FromTop(height, link.Bottom),
                link.Right,
                BottomUpSpace.FromTop(height, link.Top),
                link.Uri,
                link.Anchor,
                structureTree.LinkElementOf(link.Source)?.Id));
        }

        if (page.Watermark is not null)
        {
            watermarkRecorder.Plan(plan, page.Watermark);
        }

        return plan;
    }

    private void EmitLayer(PageRenderContext context, PageLayerKind kind, LaidOutLayer layer, double left, double top)
    {
        var body = kind == PageLayerKind.Body;
        context.Layer = (int)kind;
        foreach (var line in layer.Lines)
        {
            EmitLine(context, line, body, left, top);
        }

        if (body)
        {
            EmitTablesAndBoxes(context, layer, left, top);
            EmitImagesAndCodeSymbols(context, layer, left, top);
        }
        else
        {
            EmitImagesAndCodeSymbols(context, layer, left, top);
            EmitTablesAndBoxes(context, layer, left, top);
        }
    }

    private void EmitTablesAndBoxes(PageRenderContext context, LaidOutLayer layer, double left, double top)
    {
        OrderedMerge.VisitByOrder(
            layer.Tables,
            static table => table.ZOrder,
            layer.Boxes,
            static box => box.ZOrder,
            table => EmitTable(context, table, left, top),
            box => EmitBox(context, box, left, top));
    }

    private void EmitImagesAndCodeSymbols(PageRenderContext context, LaidOutLayer layer, double left, double top)
    {
        foreach (var image in layer.Images)
        {
            EmitImage(context, image, left, top);
        }

        foreach (var codeSymbol in layer.CodeSymbols)
        {
            EmitCodeSymbol(context, codeSymbol, left, top);
        }
    }

    private void EmitLine(
        PageRenderContext context,
        in LaidOutLine line,
        bool body,
        double left,
        double top)
    {
        var mark = context.BeginStack(
            line.ZOrder,
            PdfRect.FromSize(
                left + line.X,
                BottomUpSpace.Bottom(top, line.Y, line.Line.Height),
                line.Line.Width,
                line.Line.Height));
        if (body)
        {
            textRecorder.EmitLines(
                context, [line],
                left, top, delta: 0,
                opacity: 1, inherited: null, resolveStructure: true,
                overflowThreshold: double.PositiveInfinity);
        }
        else
        {
            textRecorder.EmitBandLines(context, [line], left, top);
        }

        context.EndStack(mark);
    }

    private void EmitImage(PageRenderContext context, in LaidOutImage image, double left, double top)
    {
        var mark = context.BeginStack(
            image.ZOrder,
            PdfRect.FromSize(
                left + image.X,
                BottomUpSpace.Bottom(top, image.Y, image.Height),
                image.Width,
                image.Height));
        imageRecorder.EmitImage(
            context, image, left, top,
            delta: 0, opacity: 1, inherited: null, inheritedArtifact: null);
        context.EndStack(mark);
    }

    private void EmitCodeSymbol(
        PageRenderContext context,
        in LaidOutCodeSymbol codeSymbol,
        double left,
        double top)
    {
        var mark = context.BeginStack(
            codeSymbol.ZOrder,
            PdfRect.FromSize(
                left + codeSymbol.X,
                BottomUpSpace.Bottom(top, codeSymbol.Y, codeSymbol.Height),
                codeSymbol.Width,
                codeSymbol.Height));
        codeSymbolRecorder.EmitCodeSymbol(context, codeSymbol, left, top);
        context.EndStack(mark);
    }

    private void EmitTable(
        PageRenderContext context,
        in LaidOutTableFragment table,
        double left,
        double top)
    {
        var mark = context.BeginStack(
            table.ZOrder,
            BottomUpSpace.Bounds(left, top, table.Bounds, delta: 0));
        tableRecorder.EmitFragment(context, table, left, top);
        context.EndStack(mark);
    }

    private void EmitBox(PageRenderContext context, LaidOutBox box, double left, double top)
    {
        var mark = context.BeginStack(
            box.ZOrder,
            BottomUpSpace.Bounds(left, top, box.Bounds, delta: 0),
            box.Transform);
        boxRecorder.EmitBox(
            context, box, BoxContentOrigin.Parent, element: null, left, top,
            delta: 0, inheritedArtifact: null);
        context.EndStack(mark);
    }

}

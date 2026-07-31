using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class DocumentGenerator
{
    private readonly RenderRequest request;
    private readonly LaidOutDocument laidOut;

    private readonly StructureTreeBuilder structureTree;
    private readonly GeneratorFontResolver fontResolver;
    private readonly ImageStore imageStore;
    private readonly TextLinePlanner textPlanner;
    private readonly TablePlanner tablePlanner;
    private readonly BoxPlanner boxPlanner;
    private readonly ImagePlanner imagePlanner;
    private readonly CodeSymbolPlanner codeSymbolPlanner;
    private readonly WatermarkDrawPlanner watermarkPlanner;

    private readonly bool markArtifacts;

    private DocumentGenerator(RenderRequest request, LaidOutDocument laidOut)
    {
        this.request = request;
        this.laidOut = laidOut;
        markArtifacts = request.Accessibility != PdfUaConformance.None
            || request.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

        structureTree = new(laidOut.Semantics, request);
        structureTree.Build();

        fontResolver = new(request.Conformance);
        imageStore = new(request.Decoders);
        textPlanner = new(
            fontResolver,
            imageStore,
            structureTree,
            request.AllowUnsupportedCharacters);
        codeSymbolPlanner = new(structureTree);
        imagePlanner = new(imageStore, structureTree);
        tablePlanner = new(structureTree);
        boxPlanner = new(structureTree);
        watermarkPlanner = new(
            fontResolver,
            imageStore,
            request.AllowUnsupportedCharacters);
    }

    internal static PortableDocument Generate(RenderRequest request, LaidOutDocument laidOut)
        => new DocumentGenerator(request, laidOut).Run();

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
        PdfModelAdapter.Apply(laidOut.Info, portable.Info);

        var paginated = laidOut.Pages;

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Length; i++)
        {
            plans.Add(GeneratePage(paginated[i]));
        }

        var fontPlans = fontResolver.Plan();
        var pages = ImmutableArray.CreateBuilder<PageOutput>(plans.Count);
        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            pages.Add(new PageContentFinalizer(structureTree, markArtifacts)
                .Finalize(plans[pageIndex], pageIndex, fontPlans));
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
            StructureTreeBuilder.Capture(structureTree.DocumentElement, pageOutputs),
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
            page.SetTextFonts(fontResolver.ExtractionFonts(pageOutput));
            portable.Pages.Insert(portable.Pages.Count, page);
        }

        return portable;
    }

    private PagePlan GeneratePage(LaidOutPage page)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var context = new EmitContext
        {
            Plan = plan,
            Text = textPlanner,
            CodeSymbols = codeSymbolPlanner,
            Images = imagePlanner,
            Tables = tablePlanner,
            Boxes = boxPlanner,
        };
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
            watermarkPlanner.Plan(plan, page.Watermark);
        }

        return plan;
    }

    private void EmitLayer(EmitContext context, PageLayerKind kind, LaidOutLayer layer, double left, double top)
    {
        var body = kind == PageLayerKind.Body;
        foreach (var content in PdfPaintOrder.ContentOf(kind))
        {
            switch (content)
            {
                case PaintContent.Lines when body:
                    textPlanner.EmitLines(
                        context, layer.Lines,
                        left, top, delta: 0,
                        opacity: 1, inherited: null, resolveStructure: true,
                        overflowThreshold: double.PositiveInfinity);
                    break;
                case PaintContent.Lines:
                    textPlanner.EmitBandLines(context, layer.Lines, left, top);
                    break;
                case PaintContent.TablesAndBoxesByOrder:
                    EmitTablesAndBoxes(context, layer.Tables, layer.Boxes, left, top);
                    break;
                case PaintContent.ImagesAndCodeSymbols:
                    EmitImagesAndCodeSymbols(context, layer, left, top);
                    break;
            }
        }
    }

    private void EmitImagesAndCodeSymbols(EmitContext context, LaidOutLayer layer, double left, double top)
    {
        foreach (var positioned in layer.Images)
        {
            imagePlanner.EmitImage(
                context, positioned, left, top,
                delta: 0, opacity: 1, inherited: null, inheritedArtifact: null);
        }

        foreach (var positioned in layer.CodeSymbols)
        {
            codeSymbolPlanner.EmitCodeSymbol(context, positioned, left, top);
        }
    }

    private void EmitTablesAndBoxes(
        EmitContext context,
        ImmutableArray<LaidOutTableFragment> tables,
        ImmutableArray<LaidOutBox> boxes,
        double left,
        double top)
    {
        OrderedMerge.VisitByOrder(
            tables,
            static table => table.ZOrder,
            boxes,
            static box => box.ZOrder,
            table => tablePlanner.EmitFragment(context, table, left, top),
            box => boxPlanner.EmitBox(
                context, box, BoxContentOrigin.Parent, element: null, left, top,
                delta: 0, inheritedArtifact: null));
    }

}

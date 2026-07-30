using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class DocumentGenerator
{
    private readonly RenderRequest request;
    private readonly LaidOutDocument laidOut;

    private readonly StructureTreeBuilder structureTree;
    private readonly GeneratorFontResolver fontResolver;
    private readonly ImageStore imageStore;
    private readonly TextLineEmitter textEmitter;
    private readonly TableEmitter tableEmitter;
    private readonly BoxEmitter boxEmitter;
    private readonly ImageEmitter imageEmitter;
    private readonly CodeSymbolEmitter codeSymbolEmitter;
    private readonly WatermarkEmitter watermarkEmitter;

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
        imageStore = new();
        textEmitter = new(
            fontResolver,
            imageStore,
            structureTree,
            laidOut.Fonts.AllowUnsupportedCharacters);
        codeSymbolEmitter = new(structureTree);
        imageEmitter = new(imageStore, structureTree);
        tableEmitter = new(imageStore, structureTree);
        boxEmitter = new(tableEmitter);
        watermarkEmitter = new(
            fontResolver,
            imageStore,
            laidOut.Fonts.AllowUnsupportedCharacters);
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
        var output = CreateOutput();
        output.FontSnapshot = laidOut.Fonts;
        output.Language = laidOut.Semantics.Language;
        PdfModelAdapter.Apply(laidOut.Info, output.Info);

        var paginated = laidOut.Pages;

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Length; i++)
        {
            plans.Add(GeneratePage(paginated[i]));
        }

        var fontPlans = fontResolver.Plan();
        var pages = ImmutableArray.CreateBuilder<PageEmissionPlan>(plans.Count);
        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            pages.Add(new PageContentFinalizer(structureTree, markArtifacts)
                .Finalize(plans[pageIndex], pageIndex, fontPlans));
        }

        var emissionPages = pages.MoveToImmutable();
        var anchors = ImmutableDictionary.CreateBuilder<string, EmissionAnchor>(System.StringComparer.Ordinal);
        for (var pageIndex = 0; pageIndex < paginated.Length; pageIndex++)
        {
            var pageHeight = paginated[pageIndex].Size.Height.Point;
            foreach (var anchor in paginated[pageIndex].Anchors)
            {
                if (!anchors.ContainsKey(anchor.Name))
                {
                    anchors.Add(
                        anchor.Name,
                        new EmissionAnchor(emissionPages[pageIndex], PageSpace.FromTop(pageHeight, anchor.Top)));
                }
            }
        }

        output.EmissionPlan = new DocumentEmissionPlan(
            emissionPages,
            StructureTreeBuilder.Capture(structureTree.DocumentElement, emissionPages),
            anchors.ToImmutable(),
            [.. request.RoleMap.Entries]);

        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            var emissionPage = emissionPages[pageIndex];
            var page = new Page(plans[pageIndex].Size.Width, plans[pageIndex].Size.Height)
            {
                EmissionIdentity = emissionPage,
            };
            page.SetLoadedContent(emissionPage.ContentArray);
            page.SetTextFonts(GeneratorFontResolver.ExtractionFonts(emissionPage));
            output.Pages.Insert(output.Pages.Count, page);
        }

        return output;
    }

    private PagePlan GeneratePage(LaidOutPage page)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var context = new EmitContext
        {
            Plan = plan,
            Text = textEmitter,
            CodeSymbols = codeSymbolEmitter,
        };
        var left = page.ContentBox.X;
        var contentTop = PageSpace.FromTop(height, page.ContentBox.Y);

        foreach (var kind in PdfPaintOrder.Layers)
        {
            var (layer, top) = kind switch
            {
                PageLayerKind.Body => (page.Body, contentTop),
                PageLayerKind.Header => (page.HeaderLayer, PageSpace.FromTop(height, page.HeaderTop)),
                _ => (page.FooterLayer, PageSpace.FromTop(height, page.FooterTop)),
            };

            EmitLayer(context, kind, layer, left, top);
        }

        foreach (var link in page.Links)
        {
            plan.Links.Add(new EmissionLink(
                link.Left,
                PageSpace.FromTop(height, link.Bottom),
                link.Right,
                PageSpace.FromTop(height, link.Top),
                link.Uri,
                link.Anchor,
                structureTree.LinkElementOf(link.Source)?.Id));
        }

        if (page.Watermark is not null)
        {
            watermarkEmitter.Plan(plan, page.Watermark);
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
                    textEmitter.EmitLines(
                        context, layer.Lines,
                        left, top, delta: 0,
                        opacity: 1, inherited: null, resolveStructure: true,
                        overflowThreshold: double.PositiveInfinity);
                    break;
                case PaintContent.Lines:
                    textEmitter.EmitBandLines(context, layer.Lines, left, top);
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
            imageEmitter.EmitImage(context, positioned, left, top);
        }

        foreach (var positioned in layer.CodeSymbols)
        {
            codeSymbolEmitter.EmitCodeSymbol(context, positioned, left, top);
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
            table => tableEmitter.EmitFragment(context, table, left, top),
            box => boxEmitter.EmitBox(context, box, left, top));
    }

}

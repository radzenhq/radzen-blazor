using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;
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

        FontEmbedding.Ensure(
            fontRegistry.SfntFaces(),
            request.AllowRestrictedEmbedding,
            request.AllowDegradedFonts);

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
                anchors.Add(
                    anchor.Name,
                    new OutputAnchor(pageOutputs[pageIndex], BottomUpSpace.FromTop(pageHeight, anchor.Top)));
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
            imageRecorder);

        SceneWalk.Page(page, new PageSceneRecorder(context, structureTree, page, height));

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

}

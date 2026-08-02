using System.Collections.Immutable;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Fonts;

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

    private DocumentRenderEngine(RenderRequest request, LaidOutDocument laidOut)
    {
        this.request = request;
        this.laidOut = laidOut;

        structureTree = new(laidOut.Semantics, request);
        structureTree.Build();

        fontRegistry = new();
        imageRegistry = new(request.Decoders);
        textRecorder = new(
            fontRegistry,
            imageRegistry,
            structureTree,
            request.AllowUnsupportedCharacters,
            request.Conformance != PdfAConformance.None || request.Accessibility != PdfUaConformance.None);
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
            ImageDecoders = request.Decoders,
        };

        output.Info.Producer = request.Producer;

        return output;
    }

    private PortableDocument Run()
    {
        var portable = CreateOutput();
        portable.FontSnapshot = laidOut.Fonts;
        portable.Language = laidOut.Semantics.Language;
        var info = laidOut.Info;
        portable.Info.Title = info.Title;
        portable.Info.Author = info.Author;
        portable.Info.Subject = info.Subject;
        portable.Info.Keywords = info.Keywords;
        portable.Info.Creator = info.Creator;
        portable.Info.CreationDate = info.CreationDate;
        portable.Info.ModificationDate = info.ModificationDate;

        var recorder = new PageSceneRecorder(
            structureTree,
            textRecorder,
            codeSymbolRecorder,
            imageRecorder,
            watermarkRecorder);

        SceneWalk.Document(laidOut, recorder);

        var plans = recorder.Plans;

        FontEmbedding.Ensure(
            fontRegistry.SfntFaces(),
            request.AllowRestrictedEmbedding);

        var fontPlans = fontRegistry.Plan();
        var pages = ImmutableArray.CreateBuilder<PageOutput>(plans.Count);
        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            using var finalizer = new PageWriter(
                structureTree,
                plans[pageIndex],
                pageIndex,
                fontPlans);
            pages.Add(finalizer.Finalize());
        }

        var pageOutputs = pages.MoveToImmutable();
        var anchors = ImmutableDictionary.CreateBuilder<string, OutputAnchor>(System.StringComparer.Ordinal);
        foreach (var anchor in recorder.Anchors)
        {
            anchors.Add(anchor.Name, new OutputAnchor(pageOutputs[anchor.PageIndex], anchor.Top));
        }

        portable.Output = new DocumentOutput(
            pageOutputs,
            structureTree.Capture(pageOutputs),
            anchors.ToImmutable(),
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
}

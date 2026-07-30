using System.Collections.Generic;
using System.Collections.Immutable;
using static Radzen.Documents.Pdf.Emit.GeneratorFontResolver;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class GeneratedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    public string Base14Name => Base14 ?? "Helvetica";

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

    public Dictionary<ushort, ushort>? CompactGidMap { get; set; }

    public Fonts.ReverseFont? Extraction { get; set; }
}

internal sealed class GeneratedImage
{
    public required string Key { get; init; }

    public required ImageXObject Image { get; init; }
}

internal sealed class GeneratedLink
{
    public required double X1 { get; init; }

    public required double Y1 { get; init; }

    public required double X2 { get; init; }

    public required double Y2 { get; init; }

    public string? Uri { get; init; }

    public string? Destination { get; init; }

    public StructureElement? Element { get; init; }
}

internal readonly record struct GeneratedAnchor(int PageIndex, double Top);

internal sealed class GeneratedExtGState
{
    public required string Key { get; init; }

    public required double FillAlpha { get; init; }

    public required double StrokeAlpha { get; init; }

    public BlendMode? Blend { get; init; }

    public GeneratedSoftMask? SoftMask { get; init; }

    public bool ClearSoftMask { get; init; }
}

internal sealed class GeneratedPattern
{
    public required string Key { get; init; }

    public required DictionaryObject Pattern { get; init; }
}

internal sealed class GeneratedPage
{
    public required byte[] Content { get; init; }

    public required IReadOnlyList<GeneratedFont> Fonts { get; init; }

    public required IReadOnlyList<GeneratedImage> Images { get; init; }

    public IReadOnlyList<GeneratedLink> Links { get; init; } = [];

    public IReadOnlyList<GeneratedExtGState> ExtGStates { get; init; } = [];

    public IReadOnlyList<GeneratedPattern> Patterns { get; init; } = [];
}

internal sealed class DocumentGenerator
{
    private readonly CapturedRendererSettings settings;
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

    private DocumentGenerator(CapturedRendererSettings settings, LaidOutDocument laidOut)
    {
        this.settings = settings;
        this.laidOut = laidOut;
        markArtifacts = settings.Accessibility != PdfUaConformance.None
            || settings.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

        structureTree = new(laidOut.Semantics, settings);
        structureTree.Build();

        fontResolver = new(settings.Conformance);
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

    public static Document Generate(Radzen.Documents.Document document, CapturedRendererSettings settings)
        => new DocumentGenerator(settings, DocumentLayouter.Layout(document)).Run();

    internal static Document Generate(CapturedRendererSettings settings, LaidOutDocument laidOut)
        => new DocumentGenerator(settings, laidOut).Run();

    private Document Run()
    {
        var output = settings.Document;
        output.FontSnapshot = laidOut.Fonts;
        output.Language = laidOut.Semantics.Language;
        PdfModelAdapter.Apply(laidOut.Info, output.Info);

        var paginated = laidOut.Pages;

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Length; i++)
        {
            plans.Add(GeneratePage(paginated[i]));
        }

        output.Structure = structureTree.DocumentElement;
        output.HasUntaggedListContent = structureTree.HasUntaggedList;
        foreach (var font in fontResolver.AllFonts)
        {
            if (font.Sfnt is { } sfnt)
            {
                font.CompactGidMap = Fonts.CompactGidMap.Build(sfnt, font.GidToUnicode.Keys);
            }
        }

        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            var plan = plans[pageIndex];
            var generated = new PageContentFinalizer(structureTree, markArtifacts).Finalize(plan, pageIndex);
            var page = new Page(plan.Size.Width, plan.Size.Height)
            {
                Generated = generated,
            };
            page.SetLoadedContent(generated.Content);
            page.SetTextFonts(BuildExtractionFonts(generated));
            output.Pages.Insert(output.Pages.Count, page);
        }

        for (var pageIndex = 0; pageIndex < paginated.Length; pageIndex++)
        {
            var pageHeight = paginated[pageIndex].Size.Height.Point;
            foreach (var anchor in paginated[pageIndex].Anchors)
            {
                if (!output.Anchors.ContainsKey(anchor.Name))
                {
                    output.Anchors[anchor.Name] = new GeneratedAnchor(pageIndex, pageHeight - anchor.Top);
                }
            }
        }

        output.AdoptMaterializedGraph(new DocumentMaterializer(output).Materialize());
        output.RoleMap = new RoleMap();
        output.Encryption = null;
        return output;
    }

    private PagePlan GeneratePage(PaginatedPage page)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var context = new EmitContext
        {
            Plan = plan,
            Text = textEmitter,
            Tables = tableEmitter,
            Images = imageEmitter,
            CodeSymbols = codeSymbolEmitter,
        };
        var left = page.ContentBox.X;
        var contentTop = height - page.ContentBox.Y;

        foreach (var kind in LaidOutPaintOrder.Layers)
        {
            var (layer, top) = kind switch
            {
                PageLayerKind.Body => (page.Body, contentTop),
                PageLayerKind.Header => (page.HeaderLayer, height - page.HeaderTop),
                _ => (page.FooterLayer, height - page.FooterTop),
            };

            EmitLayer(context, kind, layer, left, top);
        }

        foreach (var link in page.Links)
        {
            plan.Links.Add(new GeneratedLink
            {
                X1 = link.Left,
                Y1 = height - link.Bottom,
                X2 = link.Right,
                Y2 = height - link.Top,
                Uri = link.Uri,
                Destination = link.Anchor,
                Element = structureTree.ElementOf(link.Source),
            });
        }

        if (page.Watermark is not null)
        {
            watermarkEmitter.Plan(plan, page.Watermark);
        }

        return plan;
    }

    private void EmitLayer(EmitContext context, PageLayerKind kind, PageLayer layer, double left, double top)
    {
        var body = kind == PageLayerKind.Body;
        foreach (var content in LaidOutPaintOrder.ContentOf(kind))
        {
            switch (content)
            {
                case PaintContent.Lines when body:
                    textEmitter.EmitLines(
                        context, layer.Lines,
                        static l => l.Line, static l => l.Source, static _ => 0, static l => l.Y,
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

    private void EmitImagesAndCodeSymbols(EmitContext context, PageLayer layer, double left, double top)
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
        ImmutableArray<PositionedTableFragment> tables,
        ImmutableArray<PositionedBox> boxes,
        double left,
        double top)
    {
        var cursor = OrderedMerge.ByOrder(tables, static t => t.Order, boxes, static b => b.Order);
        while (cursor.MoveNext())
        {
            if (cursor.IsTable)
            {
                tableEmitter.EmitFragment(context, tables[cursor.TableIndex], left, top);
            }
            else
            {
                boxEmitter.EmitBox(context, boxes[cursor.BoxIndex], left, top);
            }
        }
    }

}

using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using static Radzen.Documents.Pdf.Emit.GeneratorFontResolver;

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
}

internal readonly record struct GeneratedAnchor(int PageIndex, double Top);

internal sealed class GeneratedExtGState
{
    public required string Key { get; init; }

    public required double FillAlpha { get; init; }

    public required double StrokeAlpha { get; init; }

    public BlendMode? Blend { get; init; }

    public bool? OverprintStroke { get; init; }

    public bool? OverprintFill { get; init; }

    public int? OverprintMode { get; init; }

    public RenderingIntent? Intent { get; init; }

    public GeneratedSoftMask? SoftMask { get; init; }

    public bool ClearSoftMask { get; init; }
}

internal sealed class GeneratedPattern
{
    public required string Key { get; init; }

    public required GradientBrush Gradient { get; init; }
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
    private readonly DocumentBuilder builder;
    private readonly CapturedBuilderSettings settings;
    private readonly FontCollection fonts;
    private readonly GeneratorFontResolver fontResolver;
    private readonly ImageStore imageStore;
    private readonly StructureTreeBuilder structureTree;
    private readonly StyleResolution resolution;
    private readonly TextLineEmitter textEmitter;
    private readonly TableEmitter tableEmitter;
    private readonly BoxEmitter boxEmitter;
    private readonly ImageEmitter imageEmitter;
    private readonly CodeEmitter codeEmitter;
    private readonly FieldResolver fieldResolver;
    private readonly WatermarkEmitter watermarkEmitter;

    private readonly bool markArtifacts;

    private DocumentGenerator(DocumentBuilder builder, CapturedBuilderSettings settings)
    {
        this.builder = builder;
        this.settings = settings;
        markArtifacts = settings.PdfUA
            || settings.Conformance is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;
        fonts = builder.Fonts;
        fontResolver = new(settings.Conformance);
        imageStore = new();
        resolution = StyleResolver.Resolve(builder);
        structureTree = new(builder, resolution);
        textEmitter = new(fonts, fontResolver, imageStore, resolution, structureTree);
        codeEmitter = new(fonts, resolution);
        imageEmitter = new(imageStore, structureTree);
        fieldResolver = new(fonts, resolution);
        var opacities = new OpacityResolver(builder);
        tableEmitter = new(imageStore, structureTree, opacities);
        boxEmitter = new(tableEmitter, opacities);
        watermarkEmitter = new(fonts, fontResolver, imageStore);
    }

    public static Document Generate(DocumentBuilder builder, CapturedBuilderSettings settings)
    {
        var generator = new DocumentGenerator(builder, settings);
        var first = generator.Run();

        if (!HasTableOfContents(builder))
        {
            return first;
        }

        var tocPages = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (name, anchor) in first.Anchors)
        {
            tocPages[name] = anchor.PageIndex + 1;
        }

        ValidateTocAnchors(builder, tocPages);
        return new DocumentGenerator(builder, settings).Run(tocPages);
    }

    private static bool HasTableOfContents(DocumentBuilder builder)
    {
        foreach (var section in builder.Sections)
        {
            foreach (var block in section.Blocks)
            {
                if (block is TableOfContents)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ValidateTocAnchors(DocumentBuilder builder, Dictionary<string, int> tocPages)
    {
        foreach (var section in builder.Sections)
        {
            foreach (var block in section.Blocks)
            {
                if (block is not TableOfContents toc)
                {
                    continue;
                }

                foreach (var entry in toc.Entries)
                {
                    if (!tocPages.ContainsKey(entry.Anchor))
                    {
                        throw new InvalidOperationException(
                            $"Table of contents entry anchor '{entry.Anchor}' does not exist; set Run.Anchor on the destination run.");
                    }
                }
            }
        }
    }

    private Document Run(IReadOnlyDictionary<string, int>? tocPages = null)
    {
        var document = settings.CreateDocument();

        structureTree.Build();

        var paginated = new List<PaginatedPage>();
        var watermarks = new List<Watermark?>();
        foreach (var section in builder.Sections)
        {
            if (section.Direction != FlowDirection.LeftToRight || section.WritingMode != WritingMode.HorizontalTopToBottom)
            {
                throw new NotSupportedException("Right-to-left flow direction and vertical writing modes are not yet supported.");
            }

            foreach (var page in Paginator.Paginate(section, fonts, imageEmitter.MeasureImage, resolution, tocPages))
            {
                paginated.Add(page);
                watermarks.Add(section.Watermark);
            }
        }

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Count; i++)
        {
            plans.Add(GeneratePage(paginated[i], i + 1, paginated.Count, watermarks[i]));
        }

        document.Structure = structureTree.DocumentElement;
        document.HasUntaggedListContent = structureTree.HasUntaggedList;
        foreach (var font in fontResolver.AllFonts)
        {
            if (font.Sfnt is { IsCff: false } sfnt)
            {
                font.CompactGidMap = GlyfSubsetter.BuildCompactGidMap(sfnt, font.GidToUnicode.Keys);
            }
            else if (font.Sfnt is { IsCff: true })
            {
                font.CompactGidMap = Fonts.Cff.CffSubsetter.BuildCompactGidMap(font.GidToUnicode.Keys);
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
            document.Pages.Insert(document.Pages.Count, page);
        }

        foreach (var (name, anchor) in textEmitter.Anchors)
        {
            document.Anchors[name] = anchor;
        }

        return document;
    }

    private PagePlan GeneratePage(PaginatedPage page, int pageNumber, int pageCount, Watermark? watermark)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var context = new EmitContext
        {
            Plan = plan,
            PageNumber = pageNumber,
            PageCount = pageCount,
            Text = textEmitter,
            Tables = tableEmitter,
            Images = imageEmitter,
            Codes = codeEmitter,
            Fields = fieldResolver,
        };
        var left = page.ContentBox.X;
        var contentTop = height - page.ContentBox.Y;
        var width = page.ContentBox.Width;

        foreach (var (layer, top, body) in new[]
        {
            (page.Body, contentTop, true),
            (page.HeaderLayer, height - page.HeaderTop, false),
            (page.FooterLayer, height - page.FooterTop, false),
        })
        {
            EmitLayer(context, layer, left, top, width, body);
        }

        if (watermark is not null)
        {
            watermarkEmitter.Plan(plan, watermark);
        }

        return plan;
    }

    private void EmitLayer(EmitContext context, PageLayer layer, double left, double top, double width, bool body)
    {
        if (!body)
        {
            textEmitter.EmitBandLines(context, layer.Lines, left, top, width);
            foreach (var positioned in layer.Images)
            {
                imageEmitter.EmitImage(context, positioned, left, top);
            }

            foreach (var positioned in layer.Codes)
            {
                codeEmitter.EmitCode(context, positioned, left, top);
            }

            EmitTablesAndBoxes(context, layer.Tables, layer.Boxes, left, top);
            return;
        }

        textEmitter.EmitFieldExpandedLines(
            context, layer.Lines,
            static l => l.Line, static l => l.Source, static _ => 0, static l => l.Y,
            left, top, delta: 0, width,
            opacity: 1, inherited: null, resolveStructure: true,
            overflowThreshold: double.PositiveInfinity);

        EmitTablesAndBoxes(context, layer.Tables, layer.Boxes, left, top);

        foreach (var positioned in layer.Images)
        {
            imageEmitter.EmitImage(context, positioned, left, top);
        }

        foreach (var positioned in layer.Codes)
        {
            codeEmitter.EmitCode(context, positioned, left, top);
        }
    }

    private void EmitTablesAndBoxes(
        EmitContext context,
        IReadOnlyList<PositionedTableFragment> tables,
        IReadOnlyList<PositionedBox> boxes,
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

using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using static Radzen.Documents.Pdf.Emit.GeneratorFontResolver;

namespace Radzen.Documents.Pdf.Emit;

// A font referenced by generated content: either a base-14 Type1 face (by PostScript
// name, WinAnsi encoded) or a registered sfnt face embedded as Type0/CID (Identity-H,
// 2-byte glyph-id codes). GidToUnicode is accumulated across the whole document so the
// shared embedded subset covers every glyph any page shows.
internal sealed class GeneratedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    // The face a non-sfnt font is emitted as; the resolver always sets Base14, so the
    // fallback only guards a GeneratedFont built with neither face.
    public string Base14Name => Base14 ?? "Helvetica";

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

    // Compact renumbering (original gid -> new gid) computed once all pages are
    // planned; content streams emit the NEW gid so CID == gid stays true for the
    // compact embedded subset (glyf and CFF alike). Null for base-14 faces.
    public Dictionary<ushort, ushort>? CompactGidMap { get; set; }

    // The shared reverse map for text extraction, built on first use once CompactGidMap
    // is final so all pages of the document reference one instance per font.
    public Fonts.ReverseFont? Extraction { get; set; }
}

internal sealed class GeneratedImage
{
    public required string Key { get; init; }

    public required ImageXObject Image { get; init; }
}

// Exactly one of Uri (external /URI action) and Destination (named destination
// resolved through the /Names /Dests tree by a /GoTo action) is set.
internal sealed class GeneratedLink
{
    public required double X1 { get; init; }

    public required double Y1 { get; init; }

    public required double X2 { get; init; }

    public required double Y2 { get; init; }

    public string? Uri { get; init; }

    public string? Destination { get; init; }
}

// Where a named anchor landed at emit time: the zero-based page and the top of
// its line in PDF user space, emitted as an /XYZ named destination on save.
internal readonly record struct GeneratedAnchor(int PageIndex, double Top);

// A page /ExtGState resource entry: constant fill (/ca) and stroke (/CA) alpha
// selected in the content stream with the gs operator. The optional blend mode,
// overprint and rendering-intent fields default to null and, when unset, emit
// exactly the alpha-only dictionary they always did.
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

    // A luminosity/alpha soft mask (/SMask << ... >>) built from a transparency group; null
    // (the default) writes no /SMask so an alpha-only state stays byte-identical.
    public GeneratedSoftMask? SoftMask { get; init; }

    // When true and SoftMask is null, writes /SMask /None to clear an inherited soft mask.
    public bool ClearSoftMask { get; init; }
}

// A page /Pattern resource entry: a shading pattern (PatternType 2) built from a
// public GradientBrush, selected in the content stream with /Pattern cs + scn.
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

// Orchestrates PDF generation: runs the merged layout engine (Paginator for paragraph
// flow, TableLayout + TablePaginator for tables) over a DocumentBuilder, plans each page
// through the element emitters, then serializes each PagePlan to a content stream. The
// per-element drawing is delegated to the emitters; this class only wires them and
// dispatches per page.
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

    // Fully tagged conformance (PDF/UA or PDF/A Level-A) forbids untagged real content, so
    // pagination and decorative draws are wrapped in /Artifact marked content. Off for plain
    // and Level-B output, which stays byte-identical.
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
        textEmitter = new(fonts, fontResolver, imageStore, resolution);
        codeEmitter = new(fonts, resolution);
        imageEmitter = new(imageStore, structureTree);
        fieldResolver = new(fonts, resolution);
        var opacities = new OpacityResolver(builder);
        tableEmitter = new(imageStore, structureTree, resolution, opacities);
        boxEmitter = new(tableEmitter, opacities);
        watermarkEmitter = new(fonts, fontResolver, imageStore);
    }

    // A document without a table of contents generates in the single pass it always had. With
    // one, the first pass resolves every anchor's page and a second pass (on a fresh generator;
    // the emitters are stateful) lays out again with the numbers substituted. The TOC line
    // footprint is independent of the digits, so both passes paginate identically.
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

    // A TableOfContents is only supported as direct section content, so a shallow scan decides
    // whether the two-pass path runs at all.
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
            // RTL / vertical shaping is not implemented; fail loudly rather than silently laying out LTR.
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

        var bodyLines = layer.Lines;
        var b = 0;
        while (b < bodyLines.Count)
        {
            var line = bodyLines[b];
            // A body paragraph carrying page-number/count fields resolves per page here,
            // the same substitution the header/footer band and band-table cell paths run.
            if (line.Source is Paragraph paragraph && fieldResolver.HasField(paragraph))
            {
                var element = structureTree.ElementOf(paragraph);
                var y = line.Y;
                foreach (var box in fieldResolver.ResolveFields(paragraph, width, context.PageNumber, context.PageCount, resolution.Alignment(paragraph)))
                {
                    textEmitter.EmitLine(context, box, left, top - y, element);
                    y += box.Height;
                }

                while (b < bodyLines.Count && bodyLines[b].Source == paragraph)
                {
                    b++;
                }
            }
            else
            {
                textEmitter.EmitLine(
                    context, line.Line, left, top - line.Y,
                    structureTree.ElementOf(line.Source),
                    markerElement: structureTree.MarkerElementOf(line.Source));
                b++;
            }
        }

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

    // Table fragments and boxes interleave by their shared placement Order (document
    // order), so a body or band mixing containers and tables paints in document order.
    // A rotated box bakes its own page-space transform inside BoxEmitter.
    private void EmitTablesAndBoxes(
        EmitContext context,
        IReadOnlyList<PositionedTableFragment> tables,
        IReadOnlyList<PositionedBox> boxes,
        double left,
        double top)
    {
        var plan = context.Plan;
        var t = 0;
        var bx = 0;
        while (t < tables.Count || bx < boxes.Count)
        {
            if (bx >= boxes.Count || (t < tables.Count && tables[t].Order <= boxes[bx].Order))
            {
                tableEmitter.EmitFragment(context, tables[t++], left, top);
            }
            else
            {
                boxEmitter.EmitBox(context, boxes[bx++], left, top);
            }
        }
    }

}

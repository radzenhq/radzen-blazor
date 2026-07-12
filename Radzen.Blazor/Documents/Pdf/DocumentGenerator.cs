using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using static Radzen.Documents.Pdf.ContentEmitter;
using static Radzen.Documents.Pdf.GeneratorFontResolver;

namespace Radzen.Documents.Pdf;

// A font referenced by generated content: either a base-14 Type1 face (by PostScript
// name, WinAnsi encoded) or a registered sfnt face embedded as Type0/CID (Identity-H,
// 2-byte glyph-id codes). GidToUnicode is accumulated across the whole document so the
// shared embedded subset covers every glyph any page shows.
internal sealed class GeneratedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

    // Compact renumbering (original gid -> new gid) computed once all pages are
    // planned; content streams emit the NEW gid so CID == gid stays true for the
    // compact embedded subset (glyf and CFF alike). Null for base-14 faces.
    public Dictionary<ushort, ushort>? CompactGidMap { get; set; }
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
// selected in the content stream with the gs operator.
internal sealed class GeneratedExtGState
{
    public required string Key { get; init; }

    public required double FillAlpha { get; init; }

    public required double StrokeAlpha { get; init; }
}

internal sealed class GeneratedPage
{
    public required byte[] Content { get; init; }

    public required IReadOnlyList<GeneratedFont> Fonts { get; init; }

    public required IReadOnlyList<GeneratedImage> Images { get; init; }

    public IReadOnlyList<GeneratedLink> Links { get; init; } = [];

    public IReadOnlyList<GeneratedExtGState> ExtGStates { get; init; } = [];
}

// Orchestrates PDF generation: runs the merged layout engine (Paginator for paragraph
// flow, TableLayout + TablePaginator for tables) over a DocumentBuilder, plans each page
// through the element emitters, then serializes each PagePlan to a content stream. The
// per-element drawing is delegated to the emitters; this class only wires them and
// dispatches per page.
internal sealed class DocumentGenerator
{
    private readonly DocumentBuilder builder;
    private readonly FontCollection fonts;
    private readonly GeneratorFontResolver fontResolver;
    private readonly ImageStore imageStore;
    private readonly StructureTreeBuilder structureTree;
    private readonly StyleResolution resolution;
    private readonly TextLineEmitter textEmitter;
    private readonly TableEmitter tableEmitter;
    private readonly ImageEmitter imageEmitter;
    private readonly CodeEmitter codeEmitter;
    private readonly FieldResolver fieldResolver;
    private readonly WatermarkEmitter watermarkEmitter;

    private DocumentGenerator(DocumentBuilder builder)
    {
        this.builder = builder;
        fonts = builder.Fonts;
        fontResolver = new(builder.Conformance);
        imageStore = new();
        structureTree = new(builder);
        resolution = StyleResolver.Resolve(builder);
        textEmitter = new(fonts, fontResolver, imageStore, resolution);
        codeEmitter = new(fonts);
        imageEmitter = new(imageStore, structureTree);
        fieldResolver = new(fonts);
        tableEmitter = new(imageStore, structureTree, resolution, new OpacityResolver(builder));
        watermarkEmitter = new(fonts, fontResolver, imageStore);
    }

    // A document without a table of contents generates in the single pass it always had. With
    // one, the first pass resolves every anchor's page and a second pass (on a fresh generator;
    // the emitters are stateful) lays out again with the numbers substituted. The TOC line
    // footprint is independent of the digits, so both passes paginate identically.
    public static Document Generate(DocumentBuilder builder)
    {
        var generator = new DocumentGenerator(builder);
        var first = generator.Run();

        if (!HasTableOfContents(builder))
        {
            return first;
        }

        var tocPages = new Dictionary<string, int>(System.StringComparer.Ordinal);
        foreach (var (name, anchor) in first.Anchors)
        {
            tocPages[name] = anchor.PageIndex + 1;
        }

        ValidateTocAnchors(builder, tocPages);
        return new DocumentGenerator(builder).Run(tocPages);
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
                        throw new System.InvalidOperationException(
                            $"Table of contents entry anchor '{entry.Anchor}' does not exist; set Run.Anchor on the destination run.");
                    }
                }
            }
        }
    }

    private Document Run(IReadOnlyDictionary<string, int>? tocPages = null)
    {
        var document = new Document { Conformance = builder.Conformance };
        document.Attachments.AddRange(builder.Attachments);
        document.Outline.AddRange(builder.Outline);
        document.Info.Title = builder.Info.Title;
        document.Info.Author = builder.Info.Author;
        document.Info.Subject = builder.Info.Subject;
        document.Info.Keywords = builder.Info.Keywords;
        document.Info.Creator = builder.Info.Creator;

        structureTree.Build();

        var paginated = new List<PaginatedPage>();
        var watermarks = new List<Watermark?>();
        foreach (var section in builder.Sections)
        {
            // RTL / vertical shaping is not implemented; fail loudly rather than silently laying out LTR.
            if (section.Direction != FlowDirection.LeftToRight || section.WritingMode != WritingMode.HorizontalTopToBottom)
            {
                throw new System.NotSupportedException("Right-to-left flow direction and vertical writing modes are not yet supported.");
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
            var generated = Finalize(plan, pageIndex);
            var page = new Page(plan.Size.Width, plan.Size.Height)
            {
                Generated = generated,
            };
            page.SetContent(generated.Content);
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

        var bodyLines = page.Lines;
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
                foreach (var box in fieldResolver.ResolveFields(paragraph, width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    textEmitter.EmitLine(context, box, left, contentTop - y, element);
                    y += box.Height;
                }

                while (b < bodyLines.Count && bodyLines[b].Source == paragraph)
                {
                    b++;
                }
            }
            else
            {
                textEmitter.EmitLine(context, line.Line, left, contentTop - line.Y, structureTree.ElementOf(line.Source));
                b++;
            }
        }

        foreach (var positioned in page.Tables)
        {
            var mark = plan.Mark();
            tableEmitter.EmitFragment(context, positioned, left, contentTop);
            if (positioned.Transform is { } transform)
            {
                plan.ApplyTransform(transform, mark);
            }
        }

        foreach (var positioned in page.Images)
        {
            imageEmitter.EmitImage(context, positioned, left, contentTop);
        }

        foreach (var positioned in page.Codes)
        {
            codeEmitter.EmitCode(context, positioned, left, contentTop);
        }

        var headerTop = height - page.HeaderTop;
        textEmitter.EmitBandLines(context, page.Header, left, headerTop, width);

        foreach (var positioned in page.HeaderImages)
        {
            imageEmitter.EmitImage(context, positioned, left, headerTop);
        }

        foreach (var positioned in page.HeaderCodes)
        {
            codeEmitter.EmitCode(context, positioned, left, headerTop);
        }

        foreach (var positioned in page.HeaderTables)
        {
            tableEmitter.EmitFragment(context, positioned, left, headerTop);
        }

        var bandTop = height - page.FooterTop;
        textEmitter.EmitBandLines(context, page.Footer, left, bandTop, width);

        foreach (var positioned in page.FooterImages)
        {
            imageEmitter.EmitImage(context, positioned, left, bandTop);
        }

        foreach (var positioned in page.FooterCodes)
        {
            codeEmitter.EmitCode(context, positioned, left, bandTop);
        }

        foreach (var positioned in page.FooterTables)
        {
            tableEmitter.EmitFragment(context, positioned, left, bandTop);
        }

        if (watermark is not null)
        {
            watermarkEmitter.Plan(plan, watermark);
        }

        return plan;
    }

    // Content emission order: fills and edges (untagged artifacts), untagged images
    // and texts (headers/footers), then tagged content grouped per structure element
    // in depth-first tree order - one BDC <</MCID n>> ... EMC per element per page -
    // so BDC order in the stream always equals the tree's reading order.
    private GeneratedPage Finalize(PagePlan plan, int pageIndex)
    {
        using var writer = new ContentWriter();

        foreach (var fill in plan.Fills)
        {
            var grouped = fill.Clip is not null || fill.ExtGState is not null;
            if (grouped)
            {
                writer.WriteRaw("q\n");
            }

            if (fill.ExtGState is { } fillState)
            {
                writer.WriteName(fillState);
                writer.WriteRaw(" gs\n");
            }

            if (fill.Clip is { } fillClip)
            {
                WriteClip(writer, fillClip, fill.ClipRadius);
            }

            writer.WriteColor(fill.Color, "rg");
            if (fill.Radius > 0)
            {
                WriteRoundedRect(writer, fill.X, fill.Y, fill.Width, fill.Height, fill.Radius);
                writer.WriteRaw("f\n");
            }
            else
            {
                writer.WriteNumber(fill.X);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Y);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Width);
                writer.WriteRaw(" ");
                writer.WriteNumber(fill.Height);
                writer.WriteRaw(" re f\n");
            }

            if (grouped)
            {
                writer.WriteRaw("Q\n");
            }
        }

        foreach (var edge in plan.Edges)
        {
            writer.WriteRaw("q\n");
            if (edge.ExtGState is { } edgeState)
            {
                writer.WriteName(edgeState);
                writer.WriteRaw(" gs\n");
            }

            if (edge.Clip is { } edgeClip)
            {
                WriteClip(writer, edgeClip, edge.ClipRadius);
            }

            writer.WriteColor(edge.Color, "RG");
            writer.WriteNumber(edge.LineWidth);
            writer.WriteRaw(" w\n");
            if (edge.Style is BorderStyle.Dashed or BorderStyle.Dotted)
            {
                var on = edge.Style == BorderStyle.Dashed ? 3.0 : 1.0;
                writer.WriteRaw("[");
                writer.WriteNumber(on * edge.LineWidth);
                writer.WriteRaw(" ");
                writer.WriteNumber(on * edge.LineWidth);
                writer.WriteRaw("] 0 d\n");
            }

            writer.WriteNumber(edge.X1);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y1);
            writer.WriteRaw(" m\n");
            writer.WriteNumber(edge.X2);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y2);
            writer.WriteRaw(" l\nS\nQ\n");
        }

        foreach (var rounded in plan.RoundedStrokes)
        {
            writer.WriteRaw("q\n");
            if (rounded.ExtGState is { } roundedState)
            {
                writer.WriteName(roundedState);
                writer.WriteRaw(" gs\n");
            }

            writer.WriteColor(rounded.Color, "RG");
            writer.WriteNumber(rounded.LineWidth);
            writer.WriteRaw(" w\n");
            if (rounded.Style is BorderStyle.Dashed or BorderStyle.Dotted)
            {
                var on = rounded.Style == BorderStyle.Dashed ? 3.0 : 1.0;
                writer.WriteRaw("[");
                writer.WriteNumber(on * rounded.LineWidth);
                writer.WriteRaw(" ");
                writer.WriteNumber(on * rounded.LineWidth);
                writer.WriteRaw("] 0 d\n");
            }

            WriteRoundedRect(writer, rounded.X, rounded.Y, rounded.Width, rounded.Height, rounded.Radius);
            writer.WriteRaw("S\nQ\n");
        }

        var taggedImages = new Dictionary<StructureElement, List<ImageDraw>>();
        var taggedTexts = new Dictionary<StructureElement, List<TextDraw>>();

        foreach (var image in plan.Images)
        {
            if (image.Element is { } element)
            {
                Accumulate(taggedImages, element, image);
            }
            else
            {
                WriteImageDraw(writer, image);
            }
        }

        foreach (var text in plan.Texts)
        {
            if (text.Element is { } element)
            {
                Accumulate(taggedTexts, element, text);
            }
            else
            {
                WriteTextDraw(writer, text);
            }
        }

        structureTree.WriteTaggedContent(writer, pageIndex, taggedImages, taggedTexts);

        if (plan.Watermark is { } watermark)
        {
            WriteWatermark(writer, watermark);
        }

        var usedFonts = new List<GeneratedFont>(plan.UsedFonts);
        var usedImages = new List<GeneratedImage>(plan.UsedImages);
        return new GeneratedPage
        {
            Content = writer.ToArray(),
            Fonts = usedFonts,
            Images = usedImages,
            Links = [.. plan.Links],
            ExtGStates = [.. plan.ExtGStates],
        };
    }

    private static void WriteWatermark(ContentWriter writer, WatermarkDraw watermark)
    {
        writer.WriteRaw("q\n");
        if (watermark.ExtGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        var radians = watermark.Rotation * System.Math.PI / 180;
        var cos = System.Math.Cos(radians);
        var sin = System.Math.Sin(radians);
        var negSin = sin == 0 ? 0 : -sin;
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(sin);
        writer.WriteRaw(" ");
        writer.WriteNumber(negSin);
        writer.WriteRaw(" ");
        writer.WriteNumber(cos);
        writer.WriteRaw(" ");
        writer.WriteNumber(watermark.CenterX);
        writer.WriteRaw(" ");
        writer.WriteNumber(watermark.CenterY);
        writer.WriteRaw(" cm\n");
        if (watermark.Image is { } image)
        {
            WriteImageDraw(writer, image);
        }

        foreach (var text in watermark.Texts)
        {
            WriteTextDraw(writer, text);
        }

        writer.WriteRaw("Q\n");
    }

    private static void Accumulate<T>(Dictionary<StructureElement, List<T>> map, StructureElement element, T draw)
    {
        if (!map.TryGetValue(element, out var list))
        {
            list = [];
            map[element] = list;
        }

        list.Add(draw);
    }
}

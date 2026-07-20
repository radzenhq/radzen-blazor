using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class StyleResolution
{
    private readonly Dictionary<Paragraph, HorizontalAlignment?> alignments = [];
    private readonly Dictionary<Run, Font> runFonts = [];
    private readonly Dictionary<Paragraph, Font> paragraphFonts = [];
    private readonly Dictionary<Barcode, Font> barcodeFonts = [];
    private readonly Dictionary<ListItem, Font> itemFonts = [];
    private readonly Dictionary<TableOfContents, Font> tocFonts = [];
    private readonly Dictionary<ListItem, (StructureElement Label, StructureElement Body)> listItemElements = [];
    private readonly Dictionary<Paragraph, (StructureElement Label, StructureElement Body)> listParagraphElements = [];

    public HorizontalAlignment? Alignment(Paragraph paragraph)
        => alignments.TryGetValue(paragraph, out var alignment) ? alignment : null;

    internal void SetAlignment(Paragraph paragraph, HorizontalAlignment? alignment)
        => alignments[paragraph] = alignment;

    public Font? RunFont(Run run)
        => runFonts.TryGetValue(run, out var font) ? font : null;

    internal void SetRunFont(Run run, Font font)
        => runFonts[run] = font;

    public Font? ParagraphFont(Paragraph paragraph)
        => paragraphFonts.TryGetValue(paragraph, out var font) ? font : null;

    internal void SetParagraphFont(Paragraph paragraph, Font font)
        => paragraphFonts[paragraph] = font;

    public Font? BarcodeFont(Barcode barcode)
        => barcodeFonts.TryGetValue(barcode, out var font) ? font : null;

    internal void SetBarcodeFont(Barcode barcode, Font font)
        => barcodeFonts[barcode] = font;

    public Font? ItemFont(ListItem item)
        => itemFonts.TryGetValue(item, out var font) ? font : null;

    internal void SetItemFont(ListItem item, Font font)
        => itemFonts[item] = font;

    public Font? TocFont(TableOfContents toc)
        => tocFonts.TryGetValue(toc, out var font) ? font : null;

    internal void SetTocFont(TableOfContents toc, Font font)
        => tocFonts[toc] = font;

    internal void SetListItemElements(ListItem item, StructureElement label, StructureElement body)
        => listItemElements[item] = (label, body);

    internal (StructureElement Label, StructureElement Body)? ListItemElements(ListItem item)
        => listItemElements.TryGetValue(item, out var elements) ? elements : null;

    internal void SetListParagraphElements(Paragraph paragraph, StructureElement label, StructureElement body)
        => listParagraphElements[paragraph] = (label, body);

    public StructureElement? BodyElementOf(Paragraph paragraph)
        => listParagraphElements.TryGetValue(paragraph, out var elements) ? elements.Body : null;

    public StructureElement? LabelElementOf(Paragraph paragraph)
        => listParagraphElements.TryGetValue(paragraph, out var elements) ? elements.Label : null;
}

internal static class StyleResolver
{
    public static StyleResolution Resolve(DocumentBuilder builder)
    {
        var resolution = new StyleResolution();
        var visitor = new StyleVisitor(builder.Styles, resolution);
        foreach (var section in builder.Sections)
        {
            ResolveBlocks(section.Blocks, [], visitor);
            ResolveBlocks(section.Header.Blocks, [], visitor);
            ResolveBlocks(section.Footer.Blocks, [], visitor);
        }

        return resolution;
    }

    private static void ResolveBlocks(BlockCollection blocks, List<Font> inherited, StyleVisitor visitor)
    {
        foreach (var block in blocks)
        {
            block.Accept(visitor, inherited);
        }
    }

    private static Font Cascade(List<Font?> sources, List<Font> inherited, StyleCollection styles)
    {
        sources.AddRange(inherited);
        sources.Add(styles.Normal.Font);
        return FontCascade.Resolve(sources);
    }

    private sealed class StyleVisitor(StyleCollection styles, StyleResolution resolution) : BlockVisitor<List<Font>, Nothing>
    {
        protected override Nothing Default(Block block, List<Font> inherited) => default;

        public override Nothing Visit(Paragraph paragraph, List<Font> inherited)
        {
            ResolveParagraph(paragraph, styles, inherited, resolution);
            return default;
        }

        public override Nothing Visit(Table table, List<Font> inherited)
        {
            ResolveTable(table, styles, inherited, this);
            return default;
        }

        public override Nothing Visit(Barcode barcode, List<Font> inherited)
        {
            ResolveBarcode(barcode, styles, inherited, resolution);
            return default;
        }

        public override Nothing Visit(List list, List<Font> inherited)
        {
            ResolveList(list, styles, inherited, resolution);
            return default;
        }

        public override Nothing Visit(Container container, List<Font> inherited)
        {
            ResolveBlocks(container.Blocks, inherited, this);
            return default;
        }

        public override Nothing Visit(TableOfContents toc, List<Font> inherited)
        {
            ResolveTableOfContents(toc, styles, inherited, resolution);
            return default;
        }
    }

    private static void ResolveList(List list, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
    {
        foreach (var item in list.Items)
        {
            resolution.SetItemFont(item, Cascade([item.Font, list.Font], inherited, styles));

            foreach (var run in item.Inlines)
            {
                resolution.SetRunFont(run, Cascade([run.Font, item.Font, list.Font], inherited, styles));
            }
        }
    }

    private static void ResolveTable(Table table, StyleCollection styles, List<Font> inherited, StyleVisitor visitor)
    {
        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                var context = new List<Font> { cell.Font };
                foreach (var style in StyleChain(cell.StyleName, styles, includeNormal: false))
                {
                    context.Add(style.Font);
                }

                context.Add(row.Font);
                context.Add(table.Font);
                context.AddRange(inherited);
                ResolveBlocks(cell.Blocks, context, visitor);
            }
        }
    }

    private static void ResolveBarcode(Barcode barcode, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
        => resolution.SetBarcodeFont(barcode, Cascade([barcode.Font], inherited, styles));

    private static void ResolveTableOfContents(TableOfContents toc, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
        => resolution.SetTocFont(toc, Cascade([toc.Font], inherited, styles));

    private static void ResolveParagraph(Paragraph paragraph, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
    {
        var chain = StyleChain(paragraph.StyleName, styles);
        HorizontalAlignment? styleAlignment = null;
        foreach (var style in chain)
        {
            if (style.AlignmentValue is { } alignment)
            {
                styleAlignment = alignment;
                break;
            }
        }

        resolution.SetAlignment(paragraph, styleAlignment);

        var namedChain = StyleChain(paragraph.StyleName, styles, includeNormal: false);

        var paragraphSources = new List<Font?> { paragraph.Font };
        foreach (var style in namedChain)
        {
            paragraphSources.Add(style.Font);
        }

        resolution.SetParagraphFont(paragraph, Cascade(paragraphSources, inherited, styles));

        foreach (var run in paragraph.Inlines)
        {
            var runSources = new List<Font?> { run.Font, paragraph.Font };
            foreach (var style in namedChain)
            {
                runSources.Add(style.Font);
            }

            resolution.SetRunFont(run, Cascade(runSources, inherited, styles));
        }
    }

    private static List<Style> StyleChain(string? name, StyleCollection styles, bool includeNormal = true)
    {
        var chain = new List<Style>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (name != null && styles.Contains(name) && visited.Add(name))
        {
            var style = styles[name];
            chain.Add(style);
            name = style.BaseStyle;
        }

        if (includeNormal && !visited.Contains(styles.Normal.Name))
        {
            chain.Add(styles.Normal);
        }

        return chain;
    }
}

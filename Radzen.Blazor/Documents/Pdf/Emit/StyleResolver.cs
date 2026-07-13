using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// The generator-owned side state of one Save: everything style resolution and pagination
// derive from the shared authoring model but must NOT write back onto it. A DocumentGenerator
// owns one of these for the duration of a single Save (exactly like it owns its GeneratedFont
// set), so nothing here is ever written onto the shared model where a concurrent Save could
// read a half-updated value. It carries, keyed by the model (or the paginator-synthesized
// paragraph) node:
//   - the style-derived alignment of every paragraph (the one non-idempotent piece);
//   - the resolved font of every run, paragraph, barcode and list item (the run -> paragraph
//     -> cell/row/table -> named-style -> Normal cascade), so the layout engine reads the
//     resolved font from here instead of off the model;
//   - the PDF/UA list structure elements (Lbl/LBody) of every list item and of the
//     paginator-synthesized marker paragraph that renders it.
internal sealed class StyleResolution
{
    private readonly Dictionary<Paragraph, HorizontalAlignment?> alignments = [];
    private readonly Dictionary<Run, Font> runFonts = [];
    private readonly Dictionary<Paragraph, Font> paragraphFonts = [];
    private readonly Dictionary<Barcode, Font> barcodeFonts = [];
    private readonly Dictionary<ListItem, Font> itemFonts = [];
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

    internal void SetListItemElements(ListItem item, StructureElement label, StructureElement body)
        => listItemElements[item] = (label, body);

    internal (StructureElement Label, StructureElement Body)? ListItemElements(ListItem item)
        => listItemElements.TryGetValue(item, out var elements) ? elements : null;

    // Records the Lbl/LBody a paginator-synthesized list-item paragraph tags into, keyed by
    // that synthesized paragraph so StructureTreeBuilder can resolve it back at emit time.
    internal void SetListParagraphElements(Paragraph paragraph, StructureElement label, StructureElement body)
        => listParagraphElements[paragraph] = (label, body);

    public StructureElement? BodyElementOf(Paragraph paragraph)
        => listParagraphElements.TryGetValue(paragraph, out var elements) ? elements.Body : null;

    public StructureElement? LabelElementOf(Paragraph paragraph)
        => listParagraphElements.TryGetValue(paragraph, out var elements) ? elements.Label : null;
}

// Resolves the effective font of every run/paragraph/barcode/list item and the style-derived
// alignment of every paragraph before layout, cascading run -> paragraph -> cell -> row ->
// named Style (walking the BaseStyle chain) -> document default (the Normal style, then the
// Font property defaults).
//
// Nothing is written onto the model: every resolved value is stored in the per-save
// StyleResolution and reaches every layout path (body, band, field, and cell/box through
// TableLayout + BoxContentLayout) from there. Nested-list items (which this pass does not
// walk) are resolved on the fly by Paginator.ExpandItem and stored into the same
// StyleResolution, so the layout engine never reads a resolved font off the shared model.
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

    // Only paragraphs, tables, barcodes, lists and (recursively) containers carry font
    // context to resolve; images, page breaks and code blocks inherit nothing (Default).
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
    }

    // List items cascade exactly like paragraph runs: item run -> item.Font -> list.Font ->
    // inherited cell/row/table context -> Normal. The item-level font (marker glyph and the
    // default for runs) omits the run override; both are stored for Paginator.ExpandItem.
    private static void ResolveList(List list, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
    {
        foreach (var item in list.Items)
        {
            var itemFont = new Font();
            itemFont.InheritFrom(item.Font);
            itemFont.InheritFrom(list.Font);
            foreach (var font in inherited)
            {
                itemFont.InheritFrom(font);
            }

            itemFont.InheritFrom(styles.Normal.Font);
            resolution.SetItemFont(item, itemFont);

            foreach (var run in item.Inlines)
            {
                var effective = new Font();
                effective.InheritFrom(run.Font);
                effective.InheritFrom(item.Font);
                effective.InheritFrom(list.Font);
                foreach (var font in inherited)
                {
                    effective.InheritFrom(font);
                }

                effective.InheritFrom(styles.Normal.Font);
                resolution.SetRunFont(run, effective);
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
                // Cell named style sits below the explicit cell font, above row/table defaults;
                // Normal is excluded here so it stays the last (document-default) fallback.
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

    // The human-readable line of a barcode inherits like a paragraph without a named style.
    private static void ResolveBarcode(Barcode barcode, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
    {
        var effective = new Font();
        effective.InheritFrom(barcode.Font);
        foreach (var font in inherited)
        {
            effective.InheritFrom(font);
        }

        effective.InheritFrom(styles.Normal.Font);
        resolution.SetBarcodeFont(barcode, effective);
    }

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

        // An explicit paragraph style outranks the ambient cell/row/table context: the
        // named-style chain (Normal excluded) is applied before the inherited fonts, so a
        // requested style wins over table defaults just as it does outside a table. Normal
        // stays the final document-default fallback. Matches word-processing semantics.
        var namedChain = StyleChain(paragraph.StyleName, styles, includeNormal: false);

        var paragraphFont = new Font();
        paragraphFont.InheritFrom(paragraph.Font);
        foreach (var style in namedChain)
        {
            paragraphFont.InheritFrom(style.Font);
        }

        foreach (var font in inherited)
        {
            paragraphFont.InheritFrom(font);
        }

        paragraphFont.InheritFrom(styles.Normal.Font);
        resolution.SetParagraphFont(paragraph, paragraphFont);

        foreach (var run in paragraph.Inlines)
        {
            var effective = new Font();
            effective.InheritFrom(run.Font);
            effective.InheritFrom(paragraph.Font);
            foreach (var style in namedChain)
            {
                effective.InheritFrom(style.Font);
            }

            foreach (var font in inherited)
            {
                effective.InheritFrom(font);
            }

            effective.InheritFrom(styles.Normal.Font);
            resolution.SetRunFont(run, effective);
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

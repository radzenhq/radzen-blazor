using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Radzen.Documents.Pdf;

// Resolves the effective font of every run and the style-derived alignment of every
// paragraph before layout, cascading run -> paragraph -> cell -> row -> named Style
// (walking the BaseStyle chain) -> document default (the Normal style, then the Font
// property defaults).
internal static class StyleResolver
{
    // The resolved item-level font of each list item (marker + content default), attached
    // here rather than on the model so Paginator.ExpandItem can consume it when it expands
    // the list into paragraphs.
    private static readonly ConditionalWeakTable<ListItem, Font> itemFonts = [];

    internal static Font? ItemFont(ListItem item)
        => itemFonts.TryGetValue(item, out var font) ? font : null;

    public static void Resolve(DocumentBuilder builder)
    {
        foreach (var section in builder.Sections)
        {
            ResolveBlocks(section.Blocks, builder.Styles, []);
            ResolveBlocks(section.Header.Blocks, builder.Styles, []);
            ResolveBlocks(section.Footer.Blocks, builder.Styles, []);
        }
    }

    private static void ResolveBlocks(BlockCollection blocks, StyleCollection styles, List<Font> inherited)
    {
        foreach (var block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                ResolveParagraph(paragraph, styles, inherited);
            }
            else if (block is Table table)
            {
                ResolveTable(table, styles, inherited);
            }
            else if (block is Barcode barcode)
            {
                ResolveBarcode(barcode, styles, inherited);
            }
            else if (block is List list)
            {
                ResolveList(list, styles, inherited);
            }
        }
    }

    // List items cascade exactly like paragraph runs: item run -> item.Font -> list.Font ->
    // inherited cell/row/table context -> Normal. The item-level font (marker glyph and the
    // default for runs) omits the run override; both are stored for Paginator.ExpandItem.
    private static void ResolveList(List list, StyleCollection styles, List<Font> inherited)
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
            itemFonts.AddOrUpdate(item, itemFont);

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
                run.EffectiveFont = effective;
            }
        }
    }

    private static void ResolveTable(Table table, StyleCollection styles, List<Font> inherited)
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
                ResolveBlocks(cell.Blocks, styles, context);
            }
        }
    }

    // The human-readable line of a barcode inherits like a paragraph without a named style.
    private static void ResolveBarcode(Barcode barcode, StyleCollection styles, List<Font> inherited)
    {
        var effective = new Font();
        effective.InheritFrom(barcode.Font);
        foreach (var font in inherited)
        {
            effective.InheritFrom(font);
        }

        effective.InheritFrom(styles.Normal.Font);
        barcode.EffectiveFont = effective;
    }

    private static void ResolveParagraph(Paragraph paragraph, StyleCollection styles, List<Font> inherited)
    {
        var chain = StyleChain(paragraph.StyleName, styles);
        paragraph.StyleAlignment = null;
        foreach (var style in chain)
        {
            if (style.AlignmentValue is { } alignment)
            {
                paragraph.StyleAlignment = alignment;
                break;
            }
        }

        var paragraphFont = new Font();
        paragraphFont.InheritFrom(paragraph.Font);
        foreach (var font in inherited)
        {
            paragraphFont.InheritFrom(font);
        }

        foreach (var style in chain)
        {
            paragraphFont.InheritFrom(style.Font);
        }

        paragraph.EffectiveFont = paragraphFont;

        foreach (var run in paragraph.Inlines)
        {
            var effective = new Font();
            effective.InheritFrom(run.Font);
            effective.InheritFrom(paragraph.Font);
            foreach (var font in inherited)
            {
                effective.InheritFrom(font);
            }

            foreach (var style in chain)
            {
                effective.InheritFrom(style.Font);
            }

            run.EffectiveFont = effective;
        }
    }

    private static List<Style> StyleChain(string? name, StyleCollection styles, bool includeNormal = true)
    {
        var chain = new List<Style>();
        var visited = new HashSet<string>(System.StringComparer.Ordinal);
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

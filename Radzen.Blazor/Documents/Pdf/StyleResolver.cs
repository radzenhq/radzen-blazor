#nullable enable
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Resolves the effective font of every run and the style-derived alignment of every
// paragraph before layout, cascading run -> paragraph -> cell -> row -> named Style
// (walking the BaseStyle chain) -> document default (the Normal style, then the Font
// property defaults).
internal static class StyleResolver
{
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
        }
    }

    private static void ResolveTable(Table table, StyleCollection styles, List<Font> inherited)
    {
        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                var context = new List<Font>(inherited.Count + 2) { cell.Font, row.Font };
                context.AddRange(inherited);
                ResolveBlocks(cell.Blocks, styles, context);
            }
        }
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

    private static List<Style> StyleChain(string? name, StyleCollection styles)
    {
        var chain = new List<Style>();
        var visited = new HashSet<string>(System.StringComparer.Ordinal);
        while (name != null && styles.Contains(name) && visited.Add(name))
        {
            var style = styles[name];
            chain.Add(style);
            name = style.BaseStyle;
        }

        if (!visited.Contains(styles.Normal.Name))
        {
            chain.Add(styles.Normal);
        }

        return chain;
    }
}

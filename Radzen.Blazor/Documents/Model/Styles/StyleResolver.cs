using System.Collections.Generic;
using System;
using Radzen.Documents.Fonts;

namespace Radzen.Documents;

internal static class StyleResolver
{
    public static StyleResolution Resolve(Document builder)
    {
        var resolution = new StyleResolutionBuilder { Opacities = new OpacityResolver(builder) };
        var visitor = new StyleVisitor(builder.Styles, resolution);
        foreach (var section in builder.Sections)
        {
            ResolveBlocks(section.Blocks, [], visitor);
            ResolveBlocks(section.Header.Blocks, [], visitor);
            ResolveBlocks(section.Footer.Blocks, [], visitor);
        }

        return resolution.Build();
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

    private sealed class StyleVisitor(StyleCollection styles, StyleResolutionBuilder resolution) : BlockVisitor<List<Font>, Nothing>
    {
        public StyleResolutionBuilder Resolution => resolution;

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
            ResolveList(list, styles, inherited, this);
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

    private static void ResolveList(List list, StyleCollection styles, List<Font> inherited, StyleVisitor visitor)
    {
        foreach (var item in list.Items)
        {
            var namedChain = StyleChain(item.StyleName, styles, includeNormal: false);
            var itemSources = new List<Font?> { item.Font };
            foreach (var style in namedChain)
            {
                itemSources.Add(style.Font);
            }

            itemSources.Add(list.Font);
            visitor.Resolution.SetItemFont(item, Cascade(itemSources, inherited, styles));

            var formatChain = StyleChain(item.StyleName, styles);
            visitor.Resolution.SetItemKeepTogether(
                item,
                item.KeepTogether ?? Chain(formatChain, static style => style.KeepTogether) ?? false);

            var context = new List<Font> { item.Font };
            foreach (var style in namedChain)
            {
                context.Add(style.Font);
            }

            context.Add(list.Font);
            context.AddRange(inherited);
            ResolveBlocks(item.Blocks, context, visitor);
        }
    }

    private static void ResolveTable(Table table, StyleCollection styles, List<Font> inherited, StyleVisitor visitor)
    {
        foreach (var row in table.Rows)
        {
            foreach (var cell in row.Cells)
            {
                var cellChain = StyleChain(cell.StyleName, styles, includeNormal: false);
                visitor.Resolution.SetCellAlignment(cell, ChainAlignment(cellChain));

                var context = new List<Font> { cell.Font };
                foreach (var style in cellChain)
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

    private static void ResolveBarcode(Barcode barcode, StyleCollection styles, List<Font> inherited, StyleResolutionBuilder resolution)
        => resolution.SetBarcodeFont(barcode, Cascade([barcode.Font], inherited, styles));

    private static void ResolveTableOfContents(TableOfContents toc, StyleCollection styles, List<Font> inherited, StyleResolutionBuilder resolution)
        => resolution.SetTocFont(toc, Cascade([toc.Font], inherited, styles));

    private static void ResolveParagraph(Paragraph paragraph, StyleCollection styles, List<Font> inherited, StyleResolutionBuilder resolution)
    {
        var chain = StyleChain(paragraph.StyleName, styles);
        var styleAlignment = ChainAlignment(chain);

        resolution.SetAlignment(paragraph, styleAlignment);
        resolution.SetHeadingLevel(paragraph, Chain(chain, static style => style.HeadingLevel) ?? 0);
        resolution.SetRole(paragraph, ChainRole(chain));
        resolution.SetFormat(paragraph, new ResolvedParagraphFormat
        {
            Alignment = paragraph.Alignment ?? styleAlignment ?? HorizontalAlignment.Left,
            SpacingBefore = paragraph.SpacingBefore ?? Chain(chain, static style => style.SpacingBefore) ?? default,
            SpacingAfter = paragraph.SpacingAfter ?? Chain(chain, static style => style.SpacingAfter) ?? default,
            LeftIndent = paragraph.LeftIndent ?? Chain(chain, static style => style.LeftIndent) ?? default,
            KeepTogether = paragraph.KeepTogether ?? Chain(chain, static style => style.KeepTogether) ?? false,
            KeepWithNext = paragraph.KeepWithNext ?? Chain(chain, static style => style.KeepWithNext) ?? false,
        });

        var namedChain = StyleChain(paragraph.StyleName, styles, includeNormal: false);

        var paragraphSources = new List<Font?> { paragraph.Font };
        foreach (var style in namedChain)
        {
            paragraphSources.Add(style.Font);
        }

        resolution.SetParagraphFont(paragraph, Cascade(paragraphSources, inherited, styles));

        foreach (var inline in paragraph.Inlines)
        {
            if (inline is not TextInline run)
            {
                continue;
            }

            var runSources = new List<Font?> { run.Font, paragraph.Font };
            foreach (var style in namedChain)
            {
                runSources.Add(style.Font);
            }

            resolution.SetRunFont(run, Cascade(runSources, inherited, styles));
        }
    }

    private static T? Chain<T>(List<Style> chain, Func<Style, T?> select) where T : struct
    {
        foreach (var style in chain)
        {
            if (select(style) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? ChainRole(List<Style> chain)
    {
        foreach (var style in chain)
        {
            if (style.Role is { } role)
            {
                return role;
            }
        }

        return null;
    }

    private static HorizontalAlignment? ChainAlignment(List<Style> chain)
        => Chain(chain, static style => style.Alignment);

    private static List<Style> StyleChain(string? name, StyleCollection styles, bool includeNormal = true)
    {
        var chain = new List<Style>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();
        var current = name;

        while (current != null)
        {
            path.Add(current);

            if (!styles.Contains(current))
            {
                throw new InvalidOperationException(
                    $"The named style '{current}' is not defined in Document.Styles. "
                    + $"Style reference chain: {string.Join(" -> ", path)}.");
            }

            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    $"The named style '{current}' takes part in a cyclic BaseStyle chain. "
                    + $"Style reference chain: {string.Join(" -> ", path)}.");
            }

            var style = styles[current];
            chain.Add(style);
            current = style.BaseStyle;
        }

        if (includeNormal && !visited.Contains(styles.Normal.Name))
        {
            chain.Add(styles.Normal);
        }

        return chain;
    }
}

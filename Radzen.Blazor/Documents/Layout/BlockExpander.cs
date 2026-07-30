using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Layout;

internal readonly record struct ListItemBlockRange(int Start, int End, bool KeepTogether);

internal sealed class ExpandedBlocks(
    IReadOnlyList<Block> blocks,
    IReadOnlyList<ListItemBlockRange> listItemRanges) : IReadOnlyList<Block>
{
    public int Count => blocks.Count;

    public Block this[int index] => blocks[index];

    public IReadOnlyList<ListItemBlockRange> ListItemRanges { get; } = listItemRanges;

    public IEnumerator<Block> GetEnumerator() => blocks.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class BlockExpander
{
    internal static ExpandedBlocks ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        LoweringContext resolution,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null)
    {
        var needsExpansion = false;
        foreach (var block in blocks)
        {
            if (block is List or Container or TableOfContents)
            {
                needsExpansion = true;
                break;
            }
        }

        if (!needsExpansion)
        {
            return new ExpandedBlocks(blocks, []);
        }

        var expanded = new List<Block>(blocks.Count);
        var ranges = new List<ListItemBlockRange>();
        var visitor = new ExpandVisitor(
            expanded,
            ranges,
            availableWidth,
            keepSpecialContainers,
            tocPages,
            fonts,
            resolution);
        foreach (var block in blocks)
        {
            block.Accept(visitor, default);
        }

        return new ExpandedBlocks(expanded, ranges);
    }

    private sealed class ExpandVisitor(
        List<Block> expanded,
        List<ListItemBlockRange> ranges,
        double availableWidth,
        bool keepSpecialContainers,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts,
        LoweringContext resolution)
        : BlockVisitor<Nothing, Nothing>
    {
        protected override Nothing Default(Block block, Nothing context)
        {
            expanded.Add(block);
            return default;
        }

        public override Nothing Visit(List list, Nothing context)
        {
            ExpandList(list, expanded, ranges, 0, null, resolution);
            return default;
        }

        public override Nothing Visit(Container container, Nothing context)
        {
            if (!keepSpecialContainers && (OverlayBoxPlacer.IsSpecial(container) || container.Rotation != 0))
            {
                throw new NotSupportedException(
                    "Overlay and rotated containers are only supported as direct section content.");
            }

            expanded.Add(container);
            return default;
        }

        public override Nothing Visit(TableOfContents toc, Nothing context)
        {
            if (!keepSpecialContainers)
            {
                throw new NotSupportedException(
                    "A table of contents is only supported as direct section content.");
            }

            ExpandTableOfContents(toc, expanded, availableWidth, tocPages, fonts, resolution);
            return default;
        }
    }

    private const string TocPagePlaceholder = "0000";

    private const double TocSentinelStop = 100000;

    private static void ExpandTableOfContents(
        TableOfContents toc,
        List<Block> expanded,
        double availableWidth,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts,
        LoweringContext resolution)
    {
        if (fonts is null)
        {
            throw new InvalidOperationException("A table of contents requires font metrics to lower.");
        }

        var font = resolution.TocFont(toc) ?? toc.Font;
        foreach (var entry in toc.Entries)
        {
            expanded.Add(LowerTocEntry(toc, entry, font, availableWidth, tocPages, fonts, resolution));
        }
    }

    private static Paragraph LowerTocEntry(
        TableOfContents toc,
        TocEntry entry,
        Font font,
        double availableWidth,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection fonts,
        LoweringContext resolution)
    {
        var indent = toc.LevelIndent.Point * entry.Level;
        var max = availableWidth - indent;
        var reserve = fonts.MeasureText(TocPagePlaceholder, font) + 2;
        var stop = Math.Max(0, max - reserve);

        var paragraph = new Paragraph { LeftIndent = Unit.FromPoint(indent) };
        paragraph.TabStops.Add(Unit.FromPoint(stop), TabAlignment.Right, toc.Leader);
        paragraph.TabStops.Add(Unit.FromPoint(TocSentinelStop));
        resolution.SetParagraphFont(paragraph, font);

        var text = SanitizeTocText(entry.Text);
        var textRun = paragraph.Inlines.Add(text);
        textRun.LinkToAnchor = entry.Anchor;
        resolution.SetRunFont(textRun, font);

        resolution.SetRunFont(paragraph.Inlines.Add("\t"), font);

        var number = tocPages is not null && tocPages.TryGetValue(entry.Anchor, out var page)
            ? page.ToString(CultureInfo.InvariantCulture)
            : TocPagePlaceholder;
        var numberRun = paragraph.Inlines.Add(number);
        numberRun.LinkToAnchor = entry.Anchor;
        resolution.SetRunFont(numberRun, font);

        if (resolution.TocEntryElement(entry) is { } reference)
        {
            resolution.SetTocParagraphElement(paragraph, reference);
        }

        if (resolution.TocLinkElement(entry) is { } link)
        {
            resolution.SetRunLinkElement(textRun, link);
            resolution.SetRunLinkElement(numberRun, link);
        }

        return paragraph;
    }

    private static string SanitizeTocText(string text)
    {
        if (text.IndexOfAny(['\t', '\r', '\n']) < 0)
        {
            return text;
        }

        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is '\t' or '\r' or '\n')
            {
                chars[i] = ' ';
            }
        }

        return new string(chars);
    }

    private static void ExpandList(
        List list,
        List<Block> expanded,
        List<ListItemBlockRange> ranges,
        double indent,
        Font? inherited,
        LoweringContext resolution)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            var item = list.Items[i];
            var start = expanded.Count;
            var contentIndent = indent + list.LeftIndent.Point + list.HangingIndent.Point;
            var itemFont = resolution.ItemFont(item) ?? ItemFont(item, list, inherited);
            foreach (var block in item.Blocks)
            {
                if (block is List nested)
                {
                    ExpandList(nested, expanded, ranges, contentIndent, itemFont, resolution);
                    continue;
                }

                if (block is Paragraph paragraph && resolution.ParagraphFont(paragraph) is null)
                {
                    var paragraphFont = FontCascade.Resolve([paragraph.Font, item.Font, list.Font, inherited]);
                    resolution.SetParagraphFont(paragraph, paragraphFont);
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is not TextInline run)
                        {
                            continue;
                        }

                        resolution.SetRunFont(
                            run,
                            resolution.RunFont(run)
                                ?? FontCascade.Resolve([run.Font, paragraph.Font, item.Font, list.Font, inherited]));
                    }
                }

                resolution.SetBlockIndent(block, contentIndent);
                expanded.Add(block);
            }

            if (expanded.Count == start)
            {
                continue;
            }

            var first = expanded[start];
            if (resolution.ListMarker(first) is null)
            {
                resolution.SetListMarker(
                    first,
                    new ListMarkerLayout(
                        Marker(list, i),
                        indent + list.LeftIndent.Point,
                        itemFont));

                if (resolution.ListItemElements(item) is { } elements)
                {
                    resolution.SetListBlockElements(first, elements.Label, elements.Body);
                }
            }

            ranges.Add(new ListItemBlockRange(
                start,
                expanded.Count - 1,
                resolution.ItemKeepTogether(item) || item.KeepTogether == true));
        }
    }

    private static Font ItemFont(ListItem item, List list, Font? inherited)
        => FontCascade.Resolve([item.Font, list.Font, inherited]);

    private const string BulletGlyph = "\u2022";

    private static string Marker(List list, int index)
        => list.Style == ListStyle.Number
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + "."
            : BulletGlyph;
}

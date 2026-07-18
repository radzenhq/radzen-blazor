using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal static class BlockExpander
{
    internal static IReadOnlyList<Block> ExpandBlocks(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null,
        StyleResolution? resolution = null)
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
            return blocks;
        }

        var expanded = new List<Block>(blocks.Count);
        var visitor = new ExpandVisitor(expanded, availableWidth, keepSpecialContainers, tocPages, fonts, resolution ?? new StyleResolution());
        foreach (var block in blocks)
        {
            block.Accept(visitor, default);
        }

        return expanded;
    }

    private sealed class ExpandVisitor(
        List<Block> expanded,
        double availableWidth,
        bool keepSpecialContainers,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts,
        StyleResolution resolution)
        : BlockVisitor<Nothing, Nothing>
    {
        protected override Nothing Default(Block block, Nothing context)
        {
            expanded.Add(block);
            return default;
        }

        public override Nothing Visit(List list, Nothing context)
        {
            ExpandList(list, expanded, 0, null, resolution);
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
        StyleResolution resolution)
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
        StyleResolution resolution)
    {
        var indent = toc.LevelIndent.Point * entry.Level;
        var max = availableWidth - indent;
        var reserve = fonts.MeasureText(TocPagePlaceholder, font) + 2;
        var stop = Math.Max(0, max - reserve);

        var paragraph = new Paragraph { LeftIndent = Unit.FromPoint(indent) };
        paragraph.TabStops.AddTabStop(Unit.FromPoint(stop), TabAlignment.Right, toc.Leader);
        paragraph.TabStops.AddTabStop(Unit.FromPoint(TocSentinelStop));
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

    private static void ExpandList(List list, List<Block> expanded, double indent, Font? inherited, StyleResolution resolution)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            var paragraph = ExpandItem(list, i, indent, inherited, resolution);
            expanded.Add(paragraph);
            if (list.Items[i].NestedList is { } nested)
            {
                ExpandList(nested, expanded, indent + list.LeftIndent.Point + list.HangingIndent.Point, resolution.ParagraphFont(paragraph), resolution);
            }
        }
    }

    private static Paragraph ExpandItem(List list, int index, double indent, Font? inherited, StyleResolution resolution)
    {
        var item = list.Items[index];

        var itemFont = resolution.ItemFont(item) ?? ItemFont(item, list, inherited);
        var paragraph = new Paragraph
        {
            LeftIndent = Unit.FromPoint(indent + list.LeftIndent.Point + list.HangingIndent.Point),
            MarkerIndent = Unit.FromPoint(indent + list.LeftIndent.Point),
            MarkerText = Marker(list, index),
        };
        resolution.SetParagraphFont(paragraph, itemFont);

        if (resolution.ListItemElements(item) is { } elements)
        {
            resolution.SetListParagraphElements(paragraph, elements.Label, elements.Body);
        }

        foreach (var run in item.Inlines)
        {
            resolution.SetRunFont(run, resolution.RunFont(run) ?? RunFont(run, item, list, inherited));
            paragraph.Inlines.Add(run);
        }

        return paragraph;
    }

    private static Font ItemFont(ListItem item, List list, Font? inherited)
        => FontCascade.Resolve([item.Font, list.Font, inherited]);

    private static Font RunFont(Run run, ListItem item, List list, Font? inherited)
        => FontCascade.Resolve([run.Font, item.Font, list.Font, inherited]);

    private const string BulletGlyph = "\u2022";

    private static string Marker(List list, int index)
        => list.Style == ListStyle.Number
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + "."
            : BulletGlyph;
}

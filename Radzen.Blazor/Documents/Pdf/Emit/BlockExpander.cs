using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

// Turns authoring blocks into the flat placeable-block sequence the paginator lays out:
// lists lower to hanging-indented marker paragraphs, a table of contents to entry
// paragraphs, and containers pass through as first-class boxes. A block sequence with no
// list/container/TOC is returned unchanged.
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

    // Lowers lists to marker paragraphs and a table of contents to entry paragraphs, keeps
    // containers first-class (rejecting overlay/rotated ones outside direct section content),
    // and passes every other block through unchanged (Default).
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
            // A Stack container is a first-class box: the section body and the
            // header/footer bands place it as a first-class box and cell/box content
            // nests it as a first-class nested box (BoxContentLayout). Overlay and
            // rotated containers are only allowed as direct section content
            // (keepSpecialContainers: true), where PlaceSpecialContainer/PlaceBox
            // handle them - nested content cannot host a page-space transform.
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

            ExpandTableOfContents(toc, expanded, availableWidth, tocPages, fonts);
            return default;
        }
    }

    // The page-number column is sized for this placeholder (plus a small safety margin) and
    // pass 1 renders it in place of the not-yet-known number, so the wrap fit of every entry
    // line is identical in both layout passes regardless of the resolved digits.
    private const string TocPagePlaceholder = "0000";

    private const double TocLeaderGap = 2.0;

    // A stop far beyond any line keeps a tab off the default 36pt grid when the entry text
    // reaches past the page-number stop: the number word then wraps in both passes alike
    // instead of depending on its (pass-varying) width against the grid.
    private const double TocSentinelStop = 100000;

    // Lowers a TableOfContents to one Paragraph per entry: linked text, a measured run of
    // leader characters and the page number right-aligned at a tab stop. See the remarks on
    // TableOfContents for why entries lower to paragraphs rather than a table.
    private static void ExpandTableOfContents(
        TableOfContents toc,
        List<Block> expanded,
        double availableWidth,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection? fonts)
    {
        if (fonts is null)
        {
            throw new InvalidOperationException("A table of contents requires font metrics to lower.");
        }

        foreach (var entry in toc.Entries)
        {
            expanded.Add(LowerTocEntry(toc, entry, availableWidth, tocPages, fonts));
        }
    }

    private static Paragraph LowerTocEntry(
        TableOfContents toc,
        TocEntry entry,
        double availableWidth,
        IReadOnlyDictionary<string, int>? tocPages,
        FontCollection fonts)
    {
        var indent = toc.LevelIndent.Point * entry.Level;
        var max = availableWidth - indent;
        var reserve = fonts.MeasureText(TocPagePlaceholder, toc.Font) + 2;
        var stop = Math.Max(0, max - reserve);

        var paragraph = new Paragraph { LeftIndent = Unit.FromPoint(indent) };
        paragraph.Font.InheritFrom(toc.Font);
        paragraph.TabStops.AddTabStop(Unit.FromPoint(stop), TabAlignment.Right);
        paragraph.TabStops.AddTabStop(Unit.FromPoint(TocSentinelStop));

        var text = SanitizeTocText(entry.Text);
        var textRun = paragraph.Inlines.Add(text);
        textRun.LinkToAnchor = entry.Anchor;
        textRun.Font.InheritFrom(toc.Font);

        var leaderWidth = fonts.MeasureText(toc.Leader.ToString(), toc.Font);
        if (leaderWidth > 0)
        {
            var textWidth = fonts.MeasureText(text, toc.Font);
            var spaceWidth = fonts.MeasureText(" ", toc.Font);
            var count = (int)Math.Floor((stop - TocLeaderGap - textWidth - spaceWidth) / leaderWidth);
            if (count >= 1)
            {
                paragraph.Inlines.Add(" " + new string(toc.Leader, count)).Font.InheritFrom(toc.Font);
            }
        }

        paragraph.Inlines.Add("\t").Font.InheritFrom(toc.Font);

        var number = tocPages is not null && tocPages.TryGetValue(entry.Anchor, out var page)
            ? page.ToString(CultureInfo.InvariantCulture)
            : TocPagePlaceholder;
        var numberRun = paragraph.Inlines.Add(number);
        numberRun.LinkToAnchor = entry.Anchor;
        numberRun.Font.InheritFrom(toc.Font);

        return paragraph;
    }

    // Tabs and line breaks in entry text would defeat the single-line tab layout; they flatten
    // to spaces.
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

    // Each nesting level shifts the marker column by the parent's LeftIndent + HangingIndent and
    // inherits the parent item's resolved font, so nested runs cascade item -> list -> parent item.
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

        // StyleResolver resolves the marker and run fonts through the full cascade (including the
        // surrounding cell/row/table context and the Normal default) and stores them in the
        // per-save StyleResolution; fall back to the item/list cascade only when the resolver has
        // not run (nested items always take this path). The resolved fonts live in the resolution
        // (keyed by the shared run and by this synthesized paragraph), never on the model.
        var itemFont = resolution.ItemFont(item) ?? ItemFont(item, list, inherited);
        var paragraph = new Paragraph
        {
            LeftIndent = Unit.FromPoint(indent + list.LeftIndent.Point + list.HangingIndent.Point),
            MarkerIndent = Unit.FromPoint(indent + list.LeftIndent.Point),
            MarkerText = Marker(list, index),
        };
        resolution.SetParagraphFont(paragraph, itemFont);

        // Null unless the tree was built for tagged output; carries the item's Lbl/LBody so the
        // synthesized paragraph tags its marker and content into the right structure elements.
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
    {
        var font = new Font();
        font.InheritFrom(item.Font);
        font.InheritFrom(list.Font);
        if (inherited != null)
        {
            font.InheritFrom(inherited);
        }

        return font;
    }

    private static Font RunFont(Run run, ListItem item, List list, Font? inherited)
    {
        var font = new Font();
        font.InheritFrom(run.Font);
        font.InheritFrom(item.Font);
        font.InheritFrom(list.Font);
        if (inherited != null)
        {
            font.InheritFrom(inherited);
        }

        return font;
    }

    private const string BulletGlyph = "\u2022";

    private static string Marker(List list, int index)
        => list.Style == ListStyle.Number
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + "."
            : BulletGlyph;
}

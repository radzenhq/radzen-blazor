using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal readonly record struct ListMarkerLayout(string Text, double Indent, Font Font);

internal sealed class StyleResolution
{
    private readonly ImmutableDictionary<Paragraph, HorizontalAlignment?> alignments;
    private readonly ImmutableDictionary<Paragraph, ResolvedParagraphFormat> formats;
    private readonly ImmutableDictionary<Cell, HorizontalAlignment?> cellAlignments;
    private readonly ImmutableDictionary<TextInline, Font> runFonts;
    private readonly ImmutableDictionary<Paragraph, Font> paragraphFonts;
    private readonly ImmutableDictionary<Barcode, Font> barcodeFonts;
    private readonly ImmutableDictionary<ListItem, Font> itemFonts;
    private readonly ImmutableDictionary<ListItem, bool> itemKeepTogether;
    private readonly ImmutableDictionary<TableOfContents, Font> tocFonts;
    private readonly ImmutableDictionary<Paragraph, int> headingLevels;
    private readonly ImmutableDictionary<Paragraph, string> roles;

    internal StyleResolution(StyleResolutionBuilder builder)
    {
        alignments = builder.Alignments.ToImmutableDictionary();
        formats = builder.Formats.ToImmutableDictionary();
        cellAlignments = builder.CellAlignments.ToImmutableDictionary();
        runFonts = builder.RunFonts.ToImmutableDictionary();
        paragraphFonts = builder.ParagraphFonts.ToImmutableDictionary();
        barcodeFonts = builder.BarcodeFonts.ToImmutableDictionary();
        itemFonts = builder.ItemFonts.ToImmutableDictionary();
        itemKeepTogether = builder.ItemKeepTogetherValues.ToImmutableDictionary();
        tocFonts = builder.TocFonts.ToImmutableDictionary();
        headingLevels = builder.HeadingLevels.ToImmutableDictionary();
        roles = builder.Roles.ToImmutableDictionary();
        Opacities = builder.Opacities;
    }

    public static StyleResolution Empty { get; } = new(new StyleResolutionBuilder());

    public OpacityResolver Opacities { get; }

    public HorizontalAlignment? Alignment(Paragraph paragraph)
        => alignments.TryGetValue(paragraph, out var alignment) ? alignment : null;

    public ResolvedParagraphFormat Format(Paragraph paragraph)
        => formats.TryGetValue(paragraph, out var format) ? format : LocalFormat(paragraph);

    public HorizontalAlignment? CellAlignment(Cell cell)
        => cellAlignments.TryGetValue(cell, out var alignment) ? alignment : null;

    public bool Contains(Paragraph paragraph) => formats.ContainsKey(paragraph);

    public static ResolvedParagraphFormat LocalFormat(Paragraph paragraph) => new()
    {
        Alignment = paragraph.Alignment ?? HorizontalAlignment.Left,
        SpacingBefore = paragraph.SpacingBefore ?? default,
        SpacingAfter = paragraph.SpacingAfter ?? default,
        LeftIndent = paragraph.LeftIndent ?? default,
        KeepTogether = paragraph.KeepTogether ?? false,
        KeepWithNext = paragraph.KeepWithNext ?? false,
    };

    public Font? RunFont(TextInline run)
        => runFonts.TryGetValue(run, out var font) ? font : null;

    public Font? ParagraphFont(Paragraph paragraph)
        => paragraphFonts.TryGetValue(paragraph, out var font) ? font : null;

    public Font? BarcodeFont(Barcode barcode)
        => barcodeFonts.TryGetValue(barcode, out var font) ? font : null;

    public Font? ItemFont(ListItem item)
        => itemFonts.TryGetValue(item, out var font) ? font : null;

    public bool ItemKeepTogether(ListItem item)
        => itemKeepTogether.TryGetValue(item, out var value) && value;

    public Font? TocFont(TableOfContents toc)
        => tocFonts.TryGetValue(toc, out var font) ? font : null;

    public int HeadingLevel(Paragraph paragraph)
        => headingLevels.TryGetValue(paragraph, out var level) ? level : 0;

    public string? Role(Paragraph paragraph)
        => roles.TryGetValue(paragraph, out var role) ? role : null;
}

internal sealed class LoweringContext
{
    private readonly Dictionary<TextInline, Font> runFonts = [];
    private readonly Dictionary<Paragraph, Font> paragraphFonts = [];
    private readonly Dictionary<ListItem, (IStructureTag Label, IStructureTag Body)> listItemElements = [];
    private readonly Dictionary<Block, (IStructureTag Label, IStructureTag Body)> listBlockElements = [];
    private readonly Dictionary<Block, double> blockIndents = [];
    private readonly Dictionary<Block, ListMarkerLayout> listMarkers = [];
    private readonly Dictionary<TocEntry, IStructureTag> tocEntryElements = [];
    private readonly Dictionary<Paragraph, IStructureTag> tocParagraphElements = [];
    private readonly Dictionary<TocEntry, IStructureTag> tocLinkElements = [];
    private readonly Dictionary<Inline, IStructureTag> runLinkElements = [];

    private LoweringContext(StyleResolution styles)
    {
        Styles = styles;
    }

    public StyleResolution Styles { get; }

    public OpacityResolver Opacities => Styles.Opacities;

    internal static LoweringContext CreateForDocument(StyleResolution styles) => new(styles);

    public HorizontalAlignment? Alignment(Paragraph paragraph)
        => Styles.Alignment(paragraph);

    public ResolvedParagraphFormat Format(Paragraph paragraph)
        => Styles.Format(paragraph);

    internal static ResolvedParagraphFormat FormatOf(LoweringContext? context, Paragraph paragraph)
        => context is null ? StyleResolution.LocalFormat(paragraph) : context.Format(paragraph);

    public HorizontalAlignment? CellAlignment(Cell cell)
        => Styles.CellAlignment(cell);

    public Font? RunFont(TextInline run)
        => runFonts.TryGetValue(run, out var font) ? font : Styles.RunFont(run);

    internal void SetRunFont(TextInline run, Font font)
        => runFonts[run] = font;

    public Font? ParagraphFont(Paragraph paragraph)
        => paragraphFonts.TryGetValue(paragraph, out var font) ? font : Styles.ParagraphFont(paragraph);

    internal void SetParagraphFont(Paragraph paragraph, Font font)
        => paragraphFonts[paragraph] = font;

    public Font? BarcodeFont(Barcode barcode)
        => Styles.BarcodeFont(barcode);

    public Font? ItemFont(ListItem item)
        => Styles.ItemFont(item);

    public bool ItemKeepTogether(ListItem item)
        => Styles.ItemKeepTogether(item);

    public Font? TocFont(TableOfContents toc)
        => Styles.TocFont(toc);

    internal void SetListItemElements(ListItem item, IStructureTag label, IStructureTag body)
        => listItemElements[item] = (label, body);

    internal (IStructureTag Label, IStructureTag Body)? ListItemElements(ListItem item)
        => listItemElements.TryGetValue(item, out var elements) ? elements : null;

    internal void SetListBlockElements(Block block, IStructureTag label, IStructureTag body)
        => listBlockElements[block] = (label, body);

    public double BlockIndent(Block block)
        => blockIndents.TryGetValue(block, out var indent) ? indent : 0;

    internal void SetBlockIndent(Block block, double indent)
        => blockIndents[block] = indent;

    public ListMarkerLayout? ListMarker(Block block)
        => listMarkers.TryGetValue(block, out var marker) ? marker : null;

    internal void SetListMarker(Block block, ListMarkerLayout marker)
        => listMarkers[block] = marker;

    internal void SetTocEntryElement(TocEntry entry, IStructureTag reference)
        => tocEntryElements[entry] = reference;

    internal IStructureTag? TocEntryElement(TocEntry entry)
        => tocEntryElements.TryGetValue(entry, out var reference) ? reference : null;

    internal void SetTocParagraphElement(Paragraph paragraph, IStructureTag reference)
        => tocParagraphElements[paragraph] = reference;

    internal void SetTocLinkElement(TocEntry entry, IStructureTag link)
        => tocLinkElements[entry] = link;

    internal IStructureTag? TocLinkElement(TocEntry entry)
        => tocLinkElements.TryGetValue(entry, out var link) ? link : null;

    internal void SetRunLinkElement(Inline inline, IStructureTag link)
        => runLinkElements[inline] = link;

    internal ImmutableArray<(Inline Inline, IStructureTag Link)> RunLinkElements()
    {
        var result = ImmutableArray.CreateBuilder<(Inline, IStructureTag)>(runLinkElements.Count);
        foreach (var (run, link) in runLinkElements)
        {
            result.Add((run, link));
        }

        return result.MoveToImmutable();
    }

    public int HeadingLevel(Paragraph paragraph)
        => Styles.HeadingLevel(paragraph);

    public string? Role(Paragraph paragraph)
        => Styles.Role(paragraph);

    internal ImmutableArray<(Paragraph Paragraph, IStructureTag Reference)> TocParagraphElements()
    {
        var result = ImmutableArray.CreateBuilder<(Paragraph, IStructureTag)>(tocParagraphElements.Count);
        foreach (var (paragraph, reference) in tocParagraphElements)
        {
            result.Add((paragraph, reference));
        }

        return result.MoveToImmutable();
    }

    internal ImmutableArray<(Block Block, IStructureTag Label, IStructureTag Body)> ListBlockElements()
    {
        var result = ImmutableArray.CreateBuilder<(Block, IStructureTag, IStructureTag)>(listBlockElements.Count);
        foreach (var (block, elements) in listBlockElements)
        {
            result.Add((block, elements.Label, elements.Body));
        }

        return result.MoveToImmutable();
    }
}

internal sealed class StyleResolutionBuilder
{
    internal Dictionary<Paragraph, HorizontalAlignment?> Alignments { get; } = [];
    internal Dictionary<Paragraph, ResolvedParagraphFormat> Formats { get; } = [];
    internal Dictionary<Cell, HorizontalAlignment?> CellAlignments { get; } = [];
    internal Dictionary<TextInline, Font> RunFonts { get; } = [];
    internal Dictionary<Paragraph, Font> ParagraphFonts { get; } = [];
    internal Dictionary<Barcode, Font> BarcodeFonts { get; } = [];
    internal Dictionary<ListItem, Font> ItemFonts { get; } = [];
    internal Dictionary<ListItem, bool> ItemKeepTogetherValues { get; } = [];
    internal Dictionary<TableOfContents, Font> TocFonts { get; } = [];
    internal Dictionary<Paragraph, int> HeadingLevels { get; } = [];
    internal Dictionary<Paragraph, string> Roles { get; } = [];

    internal OpacityResolver Opacities { get; set; } = OpacityResolver.None;

    internal void SetAlignment(Paragraph paragraph, HorizontalAlignment? alignment)
        => Alignments[paragraph] = alignment;

    internal void SetFormat(Paragraph paragraph, ResolvedParagraphFormat format)
        => Formats[paragraph] = format;

    internal void SetCellAlignment(Cell cell, HorizontalAlignment? alignment)
        => CellAlignments[cell] = alignment;

    internal void SetRunFont(TextInline run, Font font)
        => RunFonts[run] = font;

    internal void SetParagraphFont(Paragraph paragraph, Font font)
        => ParagraphFonts[paragraph] = font;

    internal void SetBarcodeFont(Barcode barcode, Font font)
        => BarcodeFonts[barcode] = font;

    internal void SetItemFont(ListItem item, Font font)
        => ItemFonts[item] = font;

    internal void SetItemKeepTogether(ListItem item, bool value)
        => ItemKeepTogetherValues[item] = value;

    internal void SetTocFont(TableOfContents toc, Font font)
        => TocFonts[toc] = font;

    internal void SetHeadingLevel(Paragraph paragraph, int level)
        => HeadingLevels[paragraph] = level;

    internal void SetRole(Paragraph paragraph, string? role)
    {
        if (role is not null)
        {
            Roles[paragraph] = role;
        }
    }

    internal StyleResolution Build() => new(this);
}

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

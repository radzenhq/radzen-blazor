using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Layout;

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

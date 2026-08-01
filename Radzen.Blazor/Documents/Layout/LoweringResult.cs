using System.Collections.Generic;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Layout;

internal readonly record struct ListMarkerLayout(string Text, double Indent, Font Font);

internal sealed class LoweringResult
{
    private readonly Dictionary<TextInline, Font> runFonts = [];
    private readonly Dictionary<Paragraph, Font> paragraphFonts = [];
    private readonly Dictionary<Block, double> blockIndents = [];
    private readonly Dictionary<Block, ListMarkerLayout> listMarkers = [];
    private readonly Dictionary<Table, TablePlacement> tablePlacements = [];

    private LoweringResult(StyleResolution styles)
    {
        Styles = styles;
    }

    public StyleResolution Styles { get; }

    public SemanticStructureHandles Semantics { get; } = new();

    public OpacityResolver Opacities => Styles.Opacities;

    internal static LoweringResult CreateForDocument(StyleResolution styles) => new(styles);

    public HorizontalAlignment? Alignment(Paragraph paragraph)
        => Styles.Alignment(paragraph);

    public ResolvedParagraphFormat Format(Paragraph paragraph)
        => Styles.Format(paragraph);

    internal static ResolvedParagraphFormat FormatOf(LoweringResult? lowering, Paragraph paragraph)
        => lowering is null ? StyleResolution.LocalFormat(paragraph) : lowering.Format(paragraph);

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

    public double BlockIndent(Block block)
        => blockIndents.TryGetValue(block, out var indent) ? indent : 0;

    internal void SetBlockIndent(Block block, double indent)
        => blockIndents[block] = indent;

    public ListMarkerLayout? ListMarker(Block block)
        => listMarkers.TryGetValue(block, out var marker) ? marker : null;

    internal void SetListMarker(Block block, ListMarkerLayout marker)
        => listMarkers[block] = marker;

    public TablePlacement TablePlacement(Table table)
    {
        if (!tablePlacements.TryGetValue(table, out var placement))
        {
            placement = global::Radzen.Documents.Layout.TablePlacement.Create(table);
            tablePlacements[table] = placement;
        }

        return placement;
    }

    public int HeadingLevel(Paragraph paragraph)
        => Styles.HeadingLevel(paragraph);

    public string? Role(Paragraph paragraph)
        => Styles.Role(paragraph);
}

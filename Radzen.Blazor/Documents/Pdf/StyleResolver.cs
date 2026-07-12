using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Radzen.Documents.Pdf;

// The side-effect-free part of one style-resolution pass: the style-derived alignment of
// every paragraph, keyed by the paragraph it belongs to. A DocumentGenerator owns one of
// these for the duration of a single Save (exactly like it owns its GeneratedFont set),
// so the alignment - the one non-idempotent piece of resolution - is never written onto
// the shared model where a concurrent Save could read a half-updated value.
internal sealed class StyleResolution
{
    public static readonly StyleResolution Empty = new();

    private readonly Dictionary<Paragraph, HorizontalAlignment?> alignments = [];

    public HorizontalAlignment? Alignment(Paragraph paragraph)
        => alignments.TryGetValue(paragraph, out var alignment) ? alignment : null;

    internal void SetAlignment(Paragraph paragraph, HorizontalAlignment? alignment)
        => alignments[paragraph] = alignment;
}

// Resolves the effective font of every run and the style-derived alignment of every
// paragraph before layout, cascading run -> paragraph -> cell -> row -> named Style
// (walking the BaseStyle chain) -> document default (the Normal style, then the Font
// property defaults).
//
// Run/paragraph/barcode fonts, the list-item font map, and cell-paragraph style alignment
// are written onto the model (and a ConditionalWeakTable) because the layout engine reads
// them off the model directly - LineBreaker and TableLayout, and TableLayout re-expands
// list items through Paginator.ExpandItem with no per-save state. Every one of those writes
// is now deterministic and single-shot: the same value every Save, never a null-then-set
// window, so concurrent Saves of one shared builder stay byte-identical. Body, band and
// field paragraphs additionally receive their alignment through the returned per-save
// StyleResolution so those paths never read the shared model at all.
internal static class StyleResolver
{
    // The resolved item-level font of each list item (marker + content default), attached
    // here rather than on the model so Paginator.ExpandItem can consume it when it expands
    // the list into paragraphs (including from TableLayout, which has no per-save state).
    private static readonly ConditionalWeakTable<ListItem, Font> itemFonts = [];

    internal static Font? ItemFont(ListItem item)
        => itemFonts.TryGetValue(item, out var font) ? font : null;

    public static StyleResolution Resolve(DocumentBuilder builder)
    {
        var resolution = new StyleResolution();
        foreach (var section in builder.Sections)
        {
            ResolveBlocks(section.Blocks, builder.Styles, [], resolution);
            ResolveBlocks(section.Header.Blocks, builder.Styles, [], resolution);
            ResolveBlocks(section.Footer.Blocks, builder.Styles, [], resolution);
        }

        return resolution;
    }

    private static void ResolveBlocks(BlockCollection blocks, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
    {
        foreach (var block in blocks)
        {
            if (block is Paragraph paragraph)
            {
                ResolveParagraph(paragraph, styles, inherited, resolution);
            }
            else if (block is Table table)
            {
                ResolveTable(table, styles, inherited, resolution);
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

    private static void ResolveTable(Table table, StyleCollection styles, List<Font> inherited, StyleResolution resolution)
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
                ResolveBlocks(cell.Blocks, styles, context, resolution);
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

        // Single assignment (no null-then-set window) so a concurrent Save reading this for a
        // cell paragraph via TableLayout never sees a transient value; the value is the same
        // every Save. Body/band/field paths read the alignment from the resolution instead.
        paragraph.StyleAlignment = styleAlignment;

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

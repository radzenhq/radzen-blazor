using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using static Radzen.Documents.Pdf.ContentEmitter;
using static Radzen.Documents.Pdf.GeneratorFontResolver;

namespace Radzen.Documents.Pdf;

// A font referenced by generated content: either a base-14 Type1 face (by PostScript
// name, WinAnsi encoded) or a registered sfnt face embedded as Type0/CID (Identity-H,
// 2-byte glyph-id codes). GidToUnicode is accumulated across the whole document so the
// shared embedded subset covers every glyph any page shows.
internal sealed class GeneratedFont
{
    public required string Key { get; init; }

    public string? Base14 { get; init; }

    public SfntFont? Sfnt { get; init; }

    public Dictionary<ushort, int> GidToUnicode { get; } = [];

    // Compact renumbering (original gid -> new gid) computed once all pages are
    // planned; content streams emit the NEW gid so CID == gid stays true for the
    // compact embedded subset (glyf and CFF alike). Null for base-14 faces.
    public Dictionary<ushort, ushort>? CompactGidMap { get; set; }
}

internal sealed class GeneratedImage
{
    public required string Key { get; init; }

    public required ImageXObject Image { get; init; }
}

internal sealed class GeneratedLink
{
    public required double X1 { get; init; }

    public required double Y1 { get; init; }

    public required double X2 { get; init; }

    public required double Y2 { get; init; }

    public required string Uri { get; init; }
}

internal sealed class GeneratedPage
{
    public required byte[] Content { get; init; }

    public required IReadOnlyList<GeneratedFont> Fonts { get; init; }

    public required IReadOnlyList<GeneratedImage> Images { get; init; }

    public IReadOnlyList<GeneratedLink> Links { get; init; } = [];
}

// Runs the merged layout engine (Paginator for paragraph flow, TableLayout +
// TablePaginator for tables) over a DocumentBuilder and emits each laid-out page as a
// physical Page whose content stream is written directly - positioned text, painted
// images and stroked cell borders - never materializing ContentElement objects.
internal sealed class DocumentGenerator
{
    private readonly DocumentBuilder builder;
    private readonly FontCollection fonts;
    private readonly List<byte> scratchBytes = [];
    private readonly GeneratorFontResolver fontResolver;
    private readonly ImageStore imageStore;
    private readonly StructureTreeBuilder structureTree;
    private readonly Dictionary<LaidOutTable, List<LaidOutCell>[]> tableRows = [];
    private StyleResolution resolution = StyleResolution.Empty;

    private DocumentGenerator(DocumentBuilder builder)
    {
        this.builder = builder;
        fonts = builder.Fonts;
        fontResolver = new(builder.Conformance);
        imageStore = new();
        structureTree = new(builder);
    }

    public static Document Generate(DocumentBuilder builder)
    {
        var generator = new DocumentGenerator(builder);
        return generator.Run();
    }

    private Document Run()
    {
        resolution = StyleResolver.Resolve(builder);

        var document = new Document { Conformance = builder.Conformance };
        document.Attachments.AddRange(builder.Attachments);
        document.Info.Title = builder.Info.Title;
        document.Info.Author = builder.Info.Author;
        document.Info.Subject = builder.Info.Subject;
        document.Info.Keywords = builder.Info.Keywords;
        document.Info.Creator = builder.Info.Creator;

        structureTree.Build();

        var paginated = new List<PaginatedPage>();
        foreach (var section in builder.Sections)
        {
            // RTL / vertical shaping is not implemented; fail loudly rather than silently laying out LTR.
            if (section.Direction != FlowDirection.LeftToRight || section.WritingMode != WritingMode.HorizontalTopToBottom)
            {
                throw new System.NotSupportedException("Right-to-left flow direction and vertical writing modes are not yet supported.");
            }

            paginated.AddRange(Paginator.Paginate(section, fonts, MeasureImage, resolution));
        }

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Count; i++)
        {
            plans.Add(GeneratePage(paginated[i], i + 1, paginated.Count));
        }

        document.Structure = structureTree.DocumentElement;

        foreach (var font in fontResolver.AllFonts)
        {
            if (font.Sfnt is { IsCff: false } sfnt)
            {
                font.CompactGidMap = GlyfSubsetter.BuildCompactGidMap(sfnt, font.GidToUnicode.Keys);
            }
            else if (font.Sfnt is { IsCff: true })
            {
                font.CompactGidMap = Fonts.Cff.CffSubsetter.BuildCompactGidMap(font.GidToUnicode.Keys);
            }
        }

        for (var pageIndex = 0; pageIndex < plans.Count; pageIndex++)
        {
            var plan = plans[pageIndex];
            var generated = Finalize(plan, pageIndex);
            var page = new Page(plan.Size.Width, plan.Size.Height)
            {
                Generated = generated,
            };
            page.SetContent(generated.Content);
            page.SetTextFonts(BuildExtractionFonts(generated));
            document.Pages.Insert(document.Pages.Count, page);
        }

        return document;
    }

    private PagePlan GeneratePage(PaginatedPage page, int pageNumber, int pageCount)
    {
        var height = page.Size.Height.Point;
        var plan = new PagePlan { Size = page.Size };
        var left = page.ContentBox.X;
        var contentTop = height - page.ContentBox.Y;
        var width = page.ContentBox.Width;

        var bodyLines = page.Lines;
        var b = 0;
        while (b < bodyLines.Count)
        {
            var line = bodyLines[b];
            // A body paragraph carrying page-number/count fields resolves per page here,
            // the same substitution the header/footer band and band-table cell paths run.
            if (line.Source is Paragraph paragraph && HasField(paragraph))
            {
                var element = structureTree.ElementOf(paragraph);
                var y = line.Y;
                foreach (var box in ResolveFields(paragraph, width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    EmitLine(plan, box, left, contentTop - y, element);
                    y += box.Height;
                }

                while (b < bodyLines.Count && bodyLines[b].Source == paragraph)
                {
                    b++;
                }
            }
            else
            {
                EmitLine(plan, line.Line, left, contentTop - line.Y, structureTree.ElementOf(line.Source));
                b++;
            }
        }

        foreach (var positioned in page.Tables)
        {
            EmitFragment(plan, positioned, left, contentTop, pageNumber, pageCount);
        }

        foreach (var positioned in page.Images)
        {
            EmitImage(plan, positioned, left, contentTop);
        }

        foreach (var positioned in page.Codes)
        {
            EmitCode(plan, positioned, left, contentTop);
        }

        var headerTop = height - page.HeaderTop;
        EmitBandLines(plan, page.Header, left, headerTop, width, pageNumber, pageCount);

        foreach (var positioned in page.HeaderImages)
        {
            EmitImage(plan, positioned, left, headerTop);
        }

        foreach (var positioned in page.HeaderCodes)
        {
            EmitCode(plan, positioned, left, headerTop);
        }

        foreach (var positioned in page.HeaderTables)
        {
            EmitFragment(plan, positioned, left, headerTop, pageNumber, pageCount);
        }

        var bandTop = height - page.FooterTop;
        EmitBandLines(plan, page.Footer, left, bandTop, width, pageNumber, pageCount);

        foreach (var positioned in page.FooterImages)
        {
            EmitImage(plan, positioned, left, bandTop);
        }

        foreach (var positioned in page.FooterCodes)
        {
            EmitCode(plan, positioned, left, bandTop);
        }

        foreach (var positioned in page.FooterTables)
        {
            EmitFragment(plan, positioned, left, bandTop, pageNumber, pageCount);
        }

        return plan;
    }

    // Header/footer bands are laid out once per section and reused on every page, so
    // a paragraph containing page-number fields is re-resolved here at emit time with
    // the actual page number and total count substituted.
    private void EmitBandLines(
        PagePlan plan,
        IReadOnlyList<PositionedLine> lines,
        double left,
        double top,
        double width,
        int pageNumber,
        int pageCount)
    {
        var i = 0;
        while (i < lines.Count)
        {
            var line = lines[i];
            if (line.Source is Paragraph paragraph && HasField(paragraph))
            {
                var y = line.Y;
                foreach (var box in ResolveFields(paragraph, width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    EmitLine(plan, box, left, top - y, null);
                    y += box.Height;
                }

                while (i < lines.Count && lines[i].Source == paragraph)
                {
                    i++;
                }
            }
            else
            {
                EmitLine(plan, line.Line, left, top - line.Y, null);
                i++;
            }
        }
    }

    private static bool SameStyle(Run a, Run b)
    {
        var fontA = a.ResolvedFont;
        var fontB = b.ResolvedFont;
        return a.Link == b.Link
            && fontA.Name == fontB.Name
            && fontA.Size == fontB.Size
            && fontA.Bold == fontB.Bold
            && fontA.Italic == fontB.Italic
            && fontA.Underline == fontB.Underline
            && fontA.Strikethrough == fontB.Strikethrough
            && fontA.Color.Equals(fontB.Color);
    }

    private static bool HasField(Paragraph paragraph)
    {
        foreach (var run in paragraph.Inlines)
        {
            if (run is PageNumberField or PageCountField)
            {
                return true;
            }
        }

        return false;
    }

    // Substitutes page-number/count fields with their resolved text, then re-runs the
    // line breaker so the paragraph wraps, honors '\n' forced breaks and tab stops, and
    // emits ALL of its lines. Consecutive runs of the same style merge into one piece so
    // the field text keeps its inter-word spaces intact (the breaker re-inserts the gaps
    // and CoalesceFragments restores them at emit); tabs are re-encoded as '\t' between
    // pieces so a trailing tab is preserved rather than dropped.
    private IReadOnlyList<LineBox> ResolveFields(
        Paragraph paragraph,
        double width,
        int pageNumber,
        int pageCount,
        HorizontalAlignment? inheritedAlignment = null)
    {
        // Text == null marks a passthrough piece: a non-text run (e.g. InlineImage) re-emitted
        // as its ORIGINAL instance so the line breaker lays it out instead of coercing it to
        // empty text.
        var pieces = new List<(Run Run, System.Text.StringBuilder? Text, int TabsBefore)>();
        var pendingTabs = 0;
        foreach (var run in paragraph.Inlines)
        {
            string text;
            switch (run)
            {
                case PageNumberField:
                    text = pageNumber.ToString(CultureInfo.InvariantCulture);
                    break;
                case PageCountField:
                    text = pageCount.ToString(CultureInfo.InvariantCulture);
                    break;
                case InlineImage:
                    // Flush a pending tab so the image keeps its tab-stop position, then re-emit
                    // the image itself rather than dropping it as its (empty) Text.
                    if (pendingTabs > 0)
                    {
                        pieces.Add((run, new System.Text.StringBuilder(), pendingTabs));
                        pendingTabs = 0;
                    }

                    pieces.Add((run, null, 0));
                    continue;
                case { } when run.GetType() == typeof(Run):
                    text = run.Text;
                    break;
                default:
                    throw new System.NotSupportedException(
                        $"ResolveFields cannot resolve inline run of type '{run!.GetType().Name}'.");
            }

            var parts = text.Split('\t');
            for (var pi = 0; pi < parts.Length; pi++)
            {
                if (pi > 0)
                {
                    pendingTabs++;
                }

                var part = parts[pi];
                if (part.Length == 0)
                {
                    continue;
                }

                if (pendingTabs == 0 && pieces.Count > 0 && pieces[^1].Text is { } previous && SameStyle(pieces[^1].Run, run))
                {
                    previous.Append(part);
                }
                else
                {
                    pieces.Add((run, new System.Text.StringBuilder(part), pendingTabs));
                    pendingTabs = 0;
                }
            }
        }

        // A trailing tab (no piece follows it) survives as a final tabs-only piece.
        if (pendingTabs > 0 && pieces.Count > 0)
        {
            pieces.Add((pieces[^1].Run, new System.Text.StringBuilder(), pendingTabs));
        }

        var resolved = new Paragraph
        {
            LeftIndent = paragraph.LeftIndent,
            LineSpacing = paragraph.LineSpacing,
            RightTabStop = paragraph.RightTabStop,
            AlignmentValue = paragraph.AlignmentValue,
            EffectiveFont = paragraph.EffectiveFont,
        };

        foreach (var stop in paragraph.TabStops)
        {
            resolved.TabStops.Add(stop);
        }

        foreach (var (run, builderText, tabsBefore) in pieces)
        {
            if (builderText is null)
            {
                resolved.Inlines.Add(run);
                continue;
            }

            resolved.Inlines.Add(new Run(new string('\t', tabsBefore) + builderText.ToString())
            {
                EffectiveFont = run.ResolvedFont,
                Link = run.Link,
            });
        }

        return LineBreaker.Break(resolved, width, fonts, inheritedAlignment);
    }

    private (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, imageStore.Decode(image).Image, availableWidth);

    private void EmitImage(PagePlan plan, PositionedImage positioned, double left, double top)
    {
        var xobject = imageStore.Decode(positioned.Source);
        plan.Images.Add(new ImageDraw
        {
            X = left + positioned.XOffset,
            Y = top - positioned.Y - positioned.Height,
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
            Element = structureTree.ElementOf(positioned.Source),
        });
        plan.UsedImages.Add(xobject);
    }

    private static readonly Color CodeBlack = Color.FromRgb(0, 0, 0);

    private void EmitCode(PagePlan plan, PositionedCode positioned, double left, double top)
        => EmitCodeBlock(plan, positioned.Source, left + positioned.XOffset, top - positioned.Y);

    private static double CodeWidth(Block code) => code switch
    {
        QrCode qr => qr.Size.Point,
        Barcode barcode => barcode.Width.Point,
        _ => 0,
    };

    private void EmitCodeBlock(PagePlan plan, Block source, double x, double topY)
    {
        switch (source)
        {
            case QrCode qr:
                EmitQrCode(plan, qr, x, topY);
                break;
            case Barcode barcode:
                EmitBarcode(plan, barcode, x, topY);
                break;
            default:
                break;
        }
    }

    // One filled square per dark module, scaled so matrix plus quiet zone fits Size x Size.
    private static void EmitQrCode(PagePlan plan, QrCode qr, double x, double topY)
    {
        var matrix = Radzen.Documents.QrEncoder.EncodeUtf8(qr.Value, qr.ErrorCorrection);
        var modules = matrix.GetLength(0);
        var quiet = System.Math.Max(0, qr.QuietZoneModules);
        var module = qr.Size.Point / (modules + (2 * quiet));

        for (var row = 0; row < modules; row++)
        {
            for (var column = 0; column < modules; column++)
            {
                if (!matrix[row, column])
                {
                    continue;
                }

                plan.Fills.Add(new FillDraw
                {
                    X = x + ((quiet + column) * module),
                    Y = topY - ((quiet + row + 1) * module),
                    Width = module,
                    Height = module,
                    Color = CodeBlack,
                });
            }
        }
    }

    // Bars come back with X/Width in modules and Y/Height in points, so only X scales.
    private void EmitBarcode(PagePlan plan, Barcode barcode, double x, double topY)
    {
        var (bars, moduleCount, _) = Radzen.Documents.BarcodeEncoder.EncodeToBars(barcode.Type, barcode.Value, barcode.Height.Point, 0);
        var scaleX = barcode.Width.Point / moduleCount;

        foreach (var bar in bars)
        {
            plan.Fills.Add(new FillDraw
            {
                X = x + (bar.X * scaleX),
                Y = topY - bar.Y - bar.Height,
                Width = bar.Width * scaleX,
                Height = bar.Height,
                Color = CodeBlack,
            });
        }

        if (!barcode.ShowText)
        {
            return;
        }

        var font = barcode.EffectiveFont ?? barcode.Font;
        var run = new Run(barcode.Value) { EffectiveFont = font };
        var paragraph = new Paragraph { AlignmentValue = HorizontalAlignment.Center, EffectiveFont = font };
        paragraph.Inlines.Add(run);

        var textTop = topY - barcode.Height.Point;
        foreach (var box in LineBreaker.Break(paragraph, barcode.Width.Point, fonts))
        {
            EmitLine(plan, box, x, textTop, null);
            textTop -= box.Height;
        }
    }

    private void EmitFragment(PagePlan plan, PositionedTableFragment positioned, double left, double contentTop, int pageNumber, int pageCount)
    {
        var layout = positioned.Layout;
        var x = left + (layout.Source?.LeftIndent.Point ?? 0);
        var rowIndex = RowIndex(layout);
        foreach (var row in positioned.Fragment.Rows)
        {
            if (layout.Source?.Rows[row.SourceRow].Background is { } background)
            {
                plan.Fills.Add(new FillDraw
                {
                    X = x,
                    Y = contentTop - (positioned.Y + row.Y + row.Height),
                    Width = layout.Width,
                    Height = row.Height,
                    Color = background,
                });
            }

            var rowCells = row.SourceRow < rowIndex.Length ? rowIndex[row.SourceRow] : null;
            if (rowCells is null)
            {
                continue;
            }

            foreach (var cell in rowCells)
            {
                var delta = positioned.Y + row.Y - cell.Bounds.Y;
                EmitCell(plan, layout, cell, x, contentTop, delta, null, pageNumber, pageCount);
            }
        }
    }

    // Groups a table's flat cell list by source row once (cached per layout) so a
    // multi-fragment table no longer rescans every cell for each row it emits.
    private List<LaidOutCell>[] RowIndex(LaidOutTable layout)
    {
        if (tableRows.TryGetValue(layout, out var cached))
        {
            return cached;
        }

        var rows = new List<LaidOutCell>[layout.RowHeights.Count];
        foreach (var cell in layout.Cells)
        {
            if (cell.Row < rows.Length)
            {
                (rows[cell.Row] ??= []).Add(cell);
            }
        }

        tableRows[layout] = rows;
        return rows;
    }

    private void EmitCell(PagePlan plan, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta, StructureElement? inherited, int pageNumber, int pageCount)
    {
        var element = structureTree.ElementOf(cell.Cell) ?? inherited;
        if (cell.Cell.Background is { } background)
        {
            plan.Fills.Add(new FillDraw
            {
                X = left + cell.Bounds.X,
                Y = contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
                Width = cell.Bounds.Width,
                Height = cell.Bounds.Height,
                Color = background,
            });
        }

        EmitBorders(plan, layout, cell, left, contentTop, delta);

        var firstText = plan.Texts.Count;
        var overflows = false;
        var cellLines = cell.Lines;
        var li = 0;
        while (li < cellLines.Count)
        {
            var line = cellLines[li];
            // Fields (page number/count) in a band-table cell resolve per page here,
            // re-broken to the cell's content width, replacing the placeholder layout.
            if (line.Source is Paragraph paragraph && HasField(paragraph))
            {
                var y = line.Y;
                foreach (var box in ResolveFields(paragraph, cell.ContentBox.Width, pageNumber, pageCount, resolution.Alignment(paragraph)))
                {
                    EmitLine(plan, box, left + line.X, contentTop - (y + delta), element);
                    overflows |= box.Width > cell.ContentBox.Width + 0.01;
                    y += box.Height;
                }

                while (li < cellLines.Count && cellLines[li].Source == paragraph)
                {
                    li++;
                }
            }
            else
            {
                EmitLine(plan, line.Line, left + line.X, contentTop - (line.Y + delta), element);
                overflows |= line.Line.Width > cell.ContentBox.Width + 0.01;
                li++;
            }
        }

        // An unbreakable token or oversized image/code wider than the cell is clipped to the
        // cell box so it never overpaints the neighboring cell.
        var cellClip = new Rect(
            left + cell.Bounds.X,
            contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
            cell.Bounds.Width,
            cell.Bounds.Height);
        if (overflows)
        {
            var texts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstText; t < texts.Length; t++)
            {
                texts[t].Clip = cellClip;
            }
        }

        var boundsLeft = cell.Bounds.X;
        var boundsRight = cell.Bounds.X + cell.Bounds.Width;
        var contentOverflows = false;
        var firstImage = plan.Images.Count;
        var firstFill = plan.Fills.Count;
        var firstCodeText = plan.Texts.Count;

        foreach (var image in cell.Images)
        {
            contentOverflows |= image.X < boundsLeft - 0.01 || image.X + image.Width > boundsRight + 0.01;
            var xobject = imageStore.Decode(image.Source);
            plan.Images.Add(new ImageDraw
            {
                X = left + image.X,
                Y = contentTop - (image.Y + delta) - image.Height,
                Width = image.Width,
                Height = image.Height,
                Image = xobject,
                Element = element,
            });
            plan.UsedImages.Add(xobject);
        }

        foreach (var code in cell.Codes)
        {
            contentOverflows |= code.X < boundsLeft - 0.01 || code.X + CodeWidth(code.Source) > boundsRight + 0.01;
            EmitCodeBlock(plan, code.Source, left + code.X, contentTop - (code.Y + delta));
        }

        if (contentOverflows)
        {
            var images = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Images);
            for (var im = firstImage; im < images.Length; im++)
            {
                images[im].Clip = cellClip;
            }

            var fills = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Fills);
            for (var f = firstFill; f < fills.Length; f++)
            {
                fills[f].Clip = cellClip;
            }

            var codeTexts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstCodeText; t < codeTexts.Length; t++)
            {
                codeTexts[t].Clip = cellClip;
            }
        }

        foreach (var nested in cell.Tables)
        {
            var nestedLeft = left + nested.X + (nested.Layout.Source?.LeftIndent.Point ?? 0);
            foreach (var nestedCell in nested.Layout.Cells)
            {
                EmitCell(plan, nested.Layout, nestedCell, nestedLeft, contentTop, delta + nested.Y, element, pageNumber, pageCount);
            }
        }
    }

    private static void EmitBorders(PagePlan plan, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta)
    {
        var cellBorders = cell.Cell.Borders;
        var rowBorders = layout.Source?.Rows[cell.Row].Borders;
        var tableBorders = layout.Source?.Borders;

        var x = left + cell.Bounds.X;
        var top = contentTop - (cell.Bounds.Y + delta);
        var right = x + cell.Bounds.Width;
        var bottom = top - cell.Bounds.Height;

        EmitEdge(plan, cellBorders.Top, rowBorders?.Top, tableBorders?.Top, x, top, right, top);
        EmitEdge(plan, cellBorders.Right, rowBorders?.Right, tableBorders?.Right, right, bottom, right, top);
        EmitEdge(plan, cellBorders.Bottom, rowBorders?.Bottom, tableBorders?.Bottom, x, bottom, right, bottom);
        EmitEdge(plan, cellBorders.Left, rowBorders?.Left, tableBorders?.Left, x, bottom, x, top);
    }

    private static void EmitEdge(
        PagePlan plan,
        Border cellEdge,
        Border? rowEdge,
        Border? tableEdge,
        double x1,
        double y1,
        double x2,
        double y2)
    {
        var edge = cellEdge;
        if (!cellEdge.IsSet)
        {
            if (rowEdge?.IsSet == true)
            {
                edge = rowEdge;
            }
            else if (tableEdge is not null)
            {
                edge = tableEdge;
            }
        }

        // MigraDoc semantics: a positive width alone makes the edge a visible solid line.
        var style = edge.Style;
        if (style == BorderStyle.None && edge.Width > 0)
        {
            style = BorderStyle.Solid;
        }

        if (style == BorderStyle.None)
        {
            return;
        }

        plan.Edges.Add(new EdgeDraw
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            LineWidth = edge.Width > 0 ? edge.Width : 0.5,
            Color = edge.Color,
            Style = style,
        });
    }

    // Consecutive fragments of the same run whose positional gap equals the source
    // whitespace between them collapse into one fragment with the spaces intact, so
    // a plain line is drawn as one text run. Tabs and justified gaps never match the
    // measured space width and keep their fragments separate.
    private List<LineFragment> CoalesceFragments(IReadOnlyList<LineFragment> fragments)
    {
        var result = new List<LineFragment>(fragments.Count);
        var spaceWidths = new Dictionary<Font, double>();
        var i = 0;
        while (i < fragments.Count)
        {
            var current = fragments[i];
            var run = current.Run;
            var text = run.Text;
            var end = current.Start + current.Length;
            var right = current.XOffset + current.Advance;
            var j = i + 1;
            while (j < fragments.Count && current.Length > 0)
            {
                var next = fragments[j];
                if (next.Run != run || next.Length == 0 || next.Start < end || next.Start > text.Length)
                {
                    break;
                }

                var allSpaces = true;
                for (var g = end; g < next.Start; g++)
                {
                    if (text[g] != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                var gapWidth = next.Start == end ? 0 : (next.Start - end) * SpaceWidth(run.ResolvedFont, spaceWidths);
                if (!allSpaces || System.Math.Abs(next.XOffset - right - gapWidth) > 0.001)
                {
                    break;
                }

                end = next.Start + next.Length;
                right = next.XOffset + next.Advance;
                j++;
            }

            if (j > i + 1)
            {
                result.Add(new LineFragment
                {
                    Run = run,
                    Text = text[current.Start..end],
                    Start = current.Start,
                    Length = end - current.Start,
                    XOffset = current.XOffset,
                    Advance = right - current.XOffset,
                });
            }
            else
            {
                result.Add(current);
            }

            i = j;
        }

        return result;
    }

    // Spaces carry no kerning, so a gap of N spaces measures as N * one space width; cache the
    // per-font single-space advance rather than measuring a fresh substring per gap.
    private double SpaceWidth(Font font, Dictionary<Font, double> cache)
    {
        if (!cache.TryGetValue(font, out var width))
        {
            width = fonts.MeasureText(" ", font);
            cache[font] = width;
        }

        return width;
    }

    private void EmitLine(PagePlan plan, LineBox line, double originX, double baseline, StructureElement? element)
    {
        var y = baseline - line.Baseline;
        var lineFragments = CoalesceFragments(line.Fragments);
        for (var fi = 0; fi < lineFragments.Count; fi++)
        {
            var fragment = lineFragments[fi];
            if (fragment.Run is InlineImage inlineImage)
            {
                EmitInlineImage(plan, inlineImage, originX + fragment.XOffset, y, element);
                continue;
            }

            var text = fragment.Text;
            if (text.Length == 0)
            {
                continue;
            }

            var font = fragment.Run.ResolvedFont;
            if (fonts.TryResolvePrimary(font, out var primary))
            {
                EmitSfntFragment(plan, fragment, primary, originX + fragment.XOffset, y, element);
            }
            else
            {
                EmitBase14Fragment(plan, fragment, font, originX + fragment.XOffset, y, element);
            }
        }

        EmitUnderlines(plan, line, originX, y);
        EmitStrikethroughs(plan, line, originX, y);
        EmitLinks(plan, line, originX, y);
    }

    // One /Link rect per maximal group of consecutive fragments of the same linked
    // run on this line; a run wrapped over several lines gets one rect per line.
    private static void EmitLinks(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var run = fragments[i].Run;
            if (run.Link is not { Length: > 0 } uri || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = start + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count && fragments[j].Run == run)
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var size = run.ResolvedFont.Size;
            plan.Links.Add(new GeneratedLink
            {
                X1 = originX + start,
                Y1 = y - (size * 0.3),
                X2 = originX + end,
                Y2 = y + (size * 0.9),
                Uri = uri,
            });

            i = j;
        }
    }

    // One underline per maximal group of consecutive fragments of the same underlined
    // run, spanning from the first fragment's start to the last fragment's end so
    // inter-word gaps inside the run stay underlined.
    private static void EmitUnderlines(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var run = fragments[i].Run;
            var font = run.ResolvedFont;
            if (!font.Underline || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = fragments[i].XOffset + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count && fragments[j].Run == run)
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var underlineY = y - (font.Size * 0.12);
            plan.Edges.Add(new EdgeDraw
            {
                X1 = originX + start,
                Y1 = underlineY,
                X2 = originX + end,
                Y2 = underlineY,
                LineWidth = System.Math.Max(font.Size * 0.06, 0.5),
                Color = font.Color,
                Style = BorderStyle.Solid,
            });

            i = j;
        }
    }

    // One strike per maximal group of consecutive strikethrough fragments that share
    // size and color (across runs), drawn at roughly the x-height midline above the
    // baseline; a change in size or color starts a new correctly-styled line.
    private static void EmitStrikethroughs(PagePlan plan, LineBox line, double originX, double y)
    {
        var fragments = line.Fragments;
        var i = 0;
        while (i < fragments.Count)
        {
            var font = fragments[i].Run.ResolvedFont;
            if (!font.Strikethrough || fragments[i].Text.Length == 0)
            {
                i++;
                continue;
            }

            var start = fragments[i].XOffset;
            var end = fragments[i].XOffset + fragments[i].Advance;
            var j = i + 1;
            while (j < fragments.Count
                && fragments[j].Run.ResolvedFont.Strikethrough
                && fragments[j].Run.ResolvedFont.Size == font.Size
                && fragments[j].Run.ResolvedFont.Color.Equals(font.Color))
            {
                end = fragments[j].XOffset + fragments[j].Advance;
                j++;
            }

            var strikeY = y + (font.Size * 0.3);
            plan.Edges.Add(new EdgeDraw
            {
                X1 = originX + start,
                Y1 = strikeY,
                X2 = originX + end,
                Y2 = strikeY,
                LineWidth = System.Math.Max(font.Size * 0.06, 0.5),
                Color = font.Color,
                Style = BorderStyle.Solid,
            });

            i = j;
        }
    }

    // Base-14 WinAnsi path. Characters outside cp1252 are never dropped: they render
    // through the registered fallback chain when it supplies a glyph, otherwise a
    // visible '?' placeholder is substituted.
    private void EmitBase14Fragment(PagePlan plan, LineFragment fragment, Font font, double startX, double y, StructureElement? element)
    {
        var metrics = Base14Metrics.Resolve(font) ?? Base14Metrics.Resolve(new Font())!;
        var size = font.Size;
        var text = fragment.Text;
        var x = startX;

        var i = 0;
        while (i < text.Length)
        {
            if (fonts.TryResolveFallbackGlyph(CodePointAt(text, i), out var face, out _) && !IsWinAnsi(CodePointAt(text, i)))
            {
                var generated = fontResolver.ResolveSfnt(face);
                var bytes = scratchBytes;
                bytes.Clear();
                var advance = 0.0;
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    if (IsWinAnsi(codepoint)
                        || !fonts.TryResolveFallbackGlyph(codepoint, out var candidate, out var gid)
                        || candidate != face)
                    {
                        break;
                    }

                    // First-seen codepoint wins so glyphs shared by several codepoints
                    // (e.g. hyphen/soft-hyphen) map deterministically, not by draw order.
                    generated.GidToUnicode.TryAdd(gid, codepoint);
                    bytes.Add((byte)(gid >> 8));
                    bytes.Add((byte)(gid & 0xFF));
                    advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                plan.UsedFonts.Add(generated);
                plan.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = y,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = [.. bytes],
                    Element = element,
                });

                x += advance;
            }
            else
            {
                var builderText = new System.Text.StringBuilder();
                while (i < text.Length)
                {
                    var codepoint = CodePointAt(text, i);
                    if (IsWinAnsi(codepoint))
                    {
                        builderText.Append((char)codepoint);
                    }
                    else if (!fonts.TryResolveFallbackGlyph(codepoint, out _, out _))
                    {
                        builderText.Append('?');
                    }
                    else
                    {
                        break;
                    }

                    i += codepoint > 0xFFFF ? 2 : 1;
                }

                var segment = builderText.ToString();
                var generated = fontResolver.ResolveBase14(font);
                plan.UsedFonts.Add(generated);
                plan.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = y,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = EncodeWinAnsi(segment),
                    Element = element,
                });

                x += metrics.MeasureString(segment, size);
            }
        }
    }

    // Splits a fragment into maximal sub-runs by the physical face that actually
    // supplies each glyph (primary or a SetFallback face), so every glyph is drawn
    // by the embedded subset that owns it - not the primary's .notdef.
    private void EmitSfntFragment(PagePlan plan, LineFragment fragment, SfntFont primary, double startX, double y, StructureElement? element)
    {
        var font = fragment.Run.ResolvedFont;
        var size = font.Size;
        var text = fragment.Text;
        var runX = startX;

        var i = 0;
        while (i < text.Length)
        {
            var (face, _) = fonts.ResolveGlyph(primary, CodePointAt(text, i));
            var generated = fontResolver.ResolveSfnt(face);
            var bytes = scratchBytes;
            bytes.Clear();
            var advance = 0.0;
            while (i < text.Length)
            {
                var codepoint = CodePointAt(text, i);
                var (candidate, gid) = fonts.ResolveGlyph(primary, codepoint);
                if (candidate != face)
                {
                    break;
                }

                // First-seen codepoint wins so glyphs shared by several codepoints
                // (e.g. hyphen/soft-hyphen) map deterministically, not by draw order.
                generated.GidToUnicode.TryAdd(gid, codepoint);
                bytes.Add((byte)(gid >> 8));
                bytes.Add((byte)(gid & 0xFF));
                advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                i += codepoint > 0xFFFF ? 2 : 1;
            }

            plan.UsedFonts.Add(generated);
            plan.Texts.Add(new TextDraw
            {
                X = runX,
                Baseline = y,
                Size = size,
                Color = font.Color,
                Font = generated,
                Bytes = [.. bytes],
                Element = element,
                // Synthetic bold: no real bold face is available, so the glyphs are
                // thickened by fill+stroke with a small stroke width at emission.
                StrokeWidth = font.Bold && !face.Bold ? size * 0.03 : 0,
                // Synthetic italic: no real italic face, so the run is slanted by a
                // sheared text matrix (tan of about 12 degrees).
                Shear = font.Italic && !face.Italic ? 0.21 : 0,
            });

            runX += advance;
        }
    }

    // Draws an inline image sitting on the line baseline: its bottom edge is at the baseline y,
    // so it advances the line by its width and shares the line height computed by the breaker.
    private void EmitInlineImage(PagePlan plan, InlineImage image, double x, double baseline, StructureElement? element)
    {
        var (width, height) = image.EffectiveSize();
        var generated = imageStore.DecodeBytes(image, image.Data);
        plan.Images.Add(new ImageDraw
        {
            X = x,
            Y = baseline,
            Width = width,
            Height = height,
            Image = generated,
            Element = element,
        });
        plan.UsedImages.Add(generated);
    }

    // Content emission order: fills and edges (untagged artifacts), untagged images
    // and texts (headers/footers), then tagged content grouped per structure element
    // in depth-first tree order - one BDC <</MCID n>> ... EMC per element per page -
    // so BDC order in the stream always equals the tree's reading order.
    private GeneratedPage Finalize(PagePlan plan, int pageIndex)
    {
        using var writer = new ContentWriter();

        foreach (var fill in plan.Fills)
        {
            if (fill.Clip is { } fillClip)
            {
                writer.WriteRaw("q\n");
                WriteClipRect(writer, fillClip);
            }

            writer.WriteColor(fill.Color, "rg");
            writer.WriteNumber(fill.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Y);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Width);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Height);
            writer.WriteRaw(" re f\n");
            if (fill.Clip is not null)
            {
                writer.WriteRaw("Q\n");
            }
        }

        foreach (var edge in plan.Edges)
        {
            writer.WriteRaw("q\n");
            writer.WriteColor(edge.Color, "RG");
            writer.WriteNumber(edge.LineWidth);
            writer.WriteRaw(" w\n");
            if (edge.Style is BorderStyle.Dashed or BorderStyle.Dotted)
            {
                var on = edge.Style == BorderStyle.Dashed ? 3.0 : 1.0;
                writer.WriteRaw("[");
                writer.WriteNumber(on * edge.LineWidth);
                writer.WriteRaw(" ");
                writer.WriteNumber(on * edge.LineWidth);
                writer.WriteRaw("] 0 d\n");
            }

            writer.WriteNumber(edge.X1);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y1);
            writer.WriteRaw(" m\n");
            writer.WriteNumber(edge.X2);
            writer.WriteRaw(" ");
            writer.WriteNumber(edge.Y2);
            writer.WriteRaw(" l\nS\nQ\n");
        }

        var taggedImages = new Dictionary<StructureElement, List<ImageDraw>>();
        var taggedTexts = new Dictionary<StructureElement, List<TextDraw>>();

        foreach (var image in plan.Images)
        {
            if (image.Element is { } element)
            {
                Accumulate(taggedImages, element, image);
            }
            else
            {
                WriteImageDraw(writer, image);
            }
        }

        foreach (var text in plan.Texts)
        {
            if (text.Element is { } element)
            {
                Accumulate(taggedTexts, element, text);
            }
            else
            {
                WriteTextDraw(writer, text);
            }
        }

        structureTree.WriteTaggedContent(writer, pageIndex, taggedImages, taggedTexts);

        var usedFonts = new List<GeneratedFont>(plan.UsedFonts);
        var usedImages = new List<GeneratedImage>(plan.UsedImages);
        return new GeneratedPage
        {
            Content = writer.ToArray(),
            Fonts = usedFonts,
            Images = usedImages,
            Links = [.. plan.Links],
        };
    }

    private static void Accumulate<T>(Dictionary<StructureElement, List<T>> map, StructureElement element, T draw)
    {
        if (!map.TryGetValue(element, out var list))
        {
            list = [];
            map[element] = list;
        }

        list.Add(draw);
    }
}

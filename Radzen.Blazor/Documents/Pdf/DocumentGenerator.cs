#nullable enable
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;

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
    private struct TextDraw
    {
        public required double X { get; init; }
        public required double Baseline { get; init; }
        public required double Size { get; init; }
        public required Color Color { get; init; }
        public required GeneratedFont Font { get; init; }
        public required byte[] Bytes { get; init; }
        public double StrokeWidth { get; init; }
        public double Shear { get; init; }
        public Rect? Clip { get; set; }
    }

    private readonly struct ImageDraw
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required GeneratedImage Image { get; init; }
    }

    private readonly struct FillDraw
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required Color Color { get; init; }
    }

    private readonly struct EdgeDraw
    {
        public required double X1 { get; init; }
        public required double Y1 { get; init; }
        public required double X2 { get; init; }
        public required double Y2 { get; init; }
        public required double LineWidth { get; init; }
        public required Color Color { get; init; }
        public required BorderStyle Style { get; init; }
    }

    private sealed class PagePlan
    {
        public required PageSize Size { get; init; }
        public List<FillDraw> Fills { get; } = [];
        public List<EdgeDraw> Edges { get; } = [];
        public List<ImageDraw> Images { get; } = [];
        public List<TextDraw> Texts { get; } = [];
        public List<GeneratedLink> Links { get; } = [];
        public HashSet<GeneratedFont> UsedFonts { get; } = [];
        public HashSet<GeneratedImage> UsedImages { get; } = [];
    }

    private readonly DocumentBuilder builder;
    private readonly FontCollection fonts;
    private readonly List<byte> scratchBytes = [];
    private readonly List<GeneratedFont> allFonts = [];
    private readonly Dictionary<string, GeneratedFont> base14Fonts = new(System.StringComparer.Ordinal);
    private readonly Dictionary<SfntFont, GeneratedFont> sfntFonts = [];
    private readonly Dictionary<Image, GeneratedImage> images = [];

    private DocumentGenerator(DocumentBuilder builder)
    {
        this.builder = builder;
        fonts = builder.Fonts;
    }

    public static Document Generate(DocumentBuilder builder)
    {
        var generator = new DocumentGenerator(builder);
        return generator.Run();
    }

    private Document Run()
    {
        StyleResolver.Resolve(builder);

        var document = new Document();
        document.Info.Title = builder.Info.Title;
        document.Info.Author = builder.Info.Author;
        document.Info.Subject = builder.Info.Subject;
        document.Info.Keywords = builder.Info.Keywords;
        document.Info.Creator = builder.Info.Creator;

        var paginated = new List<PaginatedPage>();
        foreach (var section in builder.Sections)
        {
            paginated.AddRange(Paginator.Paginate(section, fonts, MeasureImage));
        }

        var plans = new List<PagePlan>();
        for (var i = 0; i < paginated.Count; i++)
        {
            plans.Add(GeneratePage(paginated[i], i + 1, paginated.Count));
        }

        foreach (var plan in plans)
        {
            var generated = Finalize(plan);
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

        foreach (var line in page.Lines)
        {
            EmitLine(plan, line.Line, left, contentTop - line.Y);
        }

        foreach (var positioned in page.Tables)
        {
            EmitFragment(plan, positioned, left, contentTop);
        }

        foreach (var positioned in page.Images)
        {
            EmitImage(plan, positioned, left, contentTop);
        }

        var headerTop = height - page.HeaderTop;
        EmitBandLines(plan, page.Header, left, headerTop, width, pageNumber, pageCount);

        foreach (var positioned in page.HeaderImages)
        {
            EmitImage(plan, positioned, left, headerTop);
        }

        foreach (var positioned in page.HeaderTables)
        {
            EmitFragment(plan, positioned, left, headerTop);
        }

        var bandTop = height - page.FooterTop;
        EmitBandLines(plan, page.Footer, left, bandTop, width, pageNumber, pageCount);

        foreach (var positioned in page.FooterImages)
        {
            EmitImage(plan, positioned, left, bandTop);
        }

        foreach (var positioned in page.FooterTables)
        {
            EmitFragment(plan, positioned, left, bandTop);
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
                EmitLine(plan, ResolveFields(paragraph, line.Line, width, pageNumber, pageCount), left, top - line.Y);
                while (i < lines.Count && lines[i].Source == paragraph)
                {
                    i++;
                }
            }
            else
            {
                EmitLine(plan, line.Line, left, top - line.Y);
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

    // Consecutive runs of the same style merge into one fragment so the resolved
    // line is drawn as one text run with its inter-word spaces intact. Tabs split
    // fragments and advance to the default left tab stops; when the paragraph opts
    // into RightTabStop the text after the last tab is pushed flush right.
    private LineBox ResolveFields(Paragraph paragraph, LineBox template, double width, int pageNumber, int pageCount)
    {
        var pieces = new List<(Run Run, System.Text.StringBuilder Text, int TabsBefore)>();
        var pendingTabs = 0;
        foreach (var run in paragraph.Inlines)
        {
            var text = run switch
            {
                PageNumberField => pageNumber.ToString(CultureInfo.InvariantCulture),
                PageCountField => pageCount.ToString(CultureInfo.InvariantCulture),
                _ => run.Text,
            };

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

                if (pendingTabs == 0 && pieces.Count > 0 && SameStyle(pieces[^1].Run, run))
                {
                    pieces[^1].Text.Append(part);
                }
                else
                {
                    pieces.Add((run, new System.Text.StringBuilder(part), pendingTabs));
                    pendingTabs = 0;
                }
            }
        }

        var lastTab = -1;
        for (var i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].TabsBefore > 0)
            {
                lastTab = i;
            }
        }

        var fragments = new List<LineFragment>();
        double advance = 0;
        foreach (var (run, builderText, tabsBefore) in pieces)
        {
            for (var t = 0; t < tabsBefore; t++)
            {
                advance = LineBreaker.AdvanceToTabStop(advance);
            }

            var text = builderText.ToString();
            var measured = fonts.MeasureText(text, run.ResolvedFont);
            fragments.Add(new LineFragment
            {
                Run = run,
                Text = text,
                Start = 0,
                Length = text.Length,
                XOffset = advance,
                Advance = measured,
            });
            advance += measured;
        }

        var indent = paragraph.LeftIndent.Point;
        var max = width - indent;

        if (paragraph.RightTabStop && lastTab >= 0 && advance < max)
        {
            var delta = max - advance;
            var trailing = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fragments);
            for (var f = lastTab; f < trailing.Length; f++)
            {
                trailing[f].XOffset += delta;
            }

            advance = max;
        }
        var x0 = paragraph.EffectiveAlignment switch
        {
            HorizontalAlignment.Right or HorizontalAlignment.End => max - advance,
            HorizontalAlignment.Center => (max - advance) / 2.0,
            _ => 0,
        };

        var shift = indent + x0;
        if (shift != 0)
        {
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(fragments);
            for (var f = 0; f < span.Length; f++)
            {
                span[f].XOffset += shift;
            }
        }

        return new LineBox
        {
            Fragments = fragments,
            Width = advance,
            Height = template.Height,
            Baseline = template.Baseline,
        };
    }

    private (double Width, double Height) MeasureImage(Image image, double availableWidth)
        => ImageDecoder.Measure(image, Decode(image).Image, availableWidth);

    private void EmitImage(PagePlan plan, PositionedImage positioned, double left, double top)
    {
        var xobject = Decode(positioned.Source);
        plan.Images.Add(new ImageDraw
        {
            X = left,
            Y = top - positioned.Y - positioned.Height,
            Width = positioned.Width,
            Height = positioned.Height,
            Image = xobject,
        });
        plan.UsedImages.Add(xobject);
    }

    private void EmitFragment(PagePlan plan, PositionedTableFragment positioned, double left, double contentTop)
    {
        var layout = positioned.Layout;
        var x = left + (layout.Source?.LeftIndent.Point ?? 0);
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

            foreach (var cell in layout.Cells)
            {
                if (cell.Row != row.SourceRow)
                {
                    continue;
                }

                var delta = positioned.Y + row.Y - cell.Bounds.Y;
                EmitCell(plan, layout, cell, x, contentTop, delta);
            }
        }
    }

    private void EmitCell(PagePlan plan, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta)
    {
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
        foreach (var line in cell.Lines)
        {
            EmitLine(plan, line.Line, left + line.X, contentTop - (line.Y + delta));
            overflows |= line.Line.Width > cell.ContentBox.Width + 0.01;
        }

        // An unbreakable token wider than the cell is clipped to the cell box so it
        // never overpaints the neighboring cell.
        if (overflows)
        {
            var clip = new Rect(
                left + cell.Bounds.X,
                contentTop - (cell.Bounds.Y + delta) - cell.Bounds.Height,
                cell.Bounds.Width,
                cell.Bounds.Height);
            var texts = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(plan.Texts);
            for (var t = firstText; t < texts.Length; t++)
            {
                texts[t].Clip = clip;
            }
        }

        foreach (var image in cell.Images)
        {
            var xobject = Decode(image.Source);
            plan.Images.Add(new ImageDraw
            {
                X = left + image.X,
                Y = contentTop - (image.Y + delta) - image.Height,
                Width = image.Width,
                Height = image.Height,
                Image = xobject,
            });
            plan.UsedImages.Add(xobject);
        }

        foreach (var nested in cell.Tables)
        {
            var nestedLeft = left + nested.X + (nested.Layout.Source?.LeftIndent.Point ?? 0);
            foreach (var nestedCell in nested.Layout.Cells)
            {
                EmitCell(plan, nested.Layout, nestedCell, nestedLeft, contentTop, delta + nested.Y);
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

                var gap = text[end..next.Start];
                var allSpaces = true;
                foreach (var c in gap)
                {
                    if (c != ' ')
                    {
                        allSpaces = false;
                        break;
                    }
                }

                var gapWidth = gap.Length == 0 ? 0 : fonts.MeasureText(gap, run.ResolvedFont);
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

    private void EmitLine(PagePlan plan, LineBox line, double originX, double baseline)
    {
        var y = baseline - line.Baseline;
        var lineFragments = CoalesceFragments(line.Fragments);
        for (var fi = 0; fi < lineFragments.Count; fi++)
        {
            var fragment = lineFragments[fi];
            var text = fragment.Text;
            if (text.Length == 0)
            {
                continue;
            }

            var font = fragment.Run.ResolvedFont;
            if (fonts.TryResolvePrimary(font, out var primary))
            {
                EmitSfntFragment(plan, fragment, primary, originX + fragment.XOffset, y);
            }
            else
            {
                EmitBase14Fragment(plan, fragment, font, originX + fragment.XOffset, y);
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

    // One strike per maximal group of consecutive strikethrough fragments (across
    // runs), drawn at roughly the x-height midline above the baseline.
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
            while (j < fragments.Count && fragments[j].Run.ResolvedFont.Strikethrough)
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
    private void EmitBase14Fragment(PagePlan plan, LineFragment fragment, Font font, double startX, double y)
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
                var generated = ResolveSfnt(face);
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

                    generated.GidToUnicode[gid] = codepoint;
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
                var generated = ResolveBase14(font);
                plan.UsedFonts.Add(generated);
                plan.Texts.Add(new TextDraw
                {
                    X = x,
                    Baseline = y,
                    Size = size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = EncodeWinAnsi(segment),
                });

                x += metrics.MeasureString(segment, size);
            }
        }
    }

    // Splits a fragment into maximal sub-runs by the physical face that actually
    // supplies each glyph (primary or a SetFallback face), so every glyph is drawn
    // by the embedded subset that owns it - not the primary's .notdef.
    private void EmitSfntFragment(PagePlan plan, LineFragment fragment, SfntFont primary, double startX, double y)
    {
        var font = fragment.Run.ResolvedFont;
        var size = font.Size;
        var text = fragment.Text;
        var runX = startX;

        var i = 0;
        while (i < text.Length)
        {
            var (face, _) = fonts.ResolveGlyph(primary, CodePointAt(text, i));
            var generated = ResolveSfnt(face);
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

                generated.GidToUnicode[gid] = codepoint;
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

    private static int CodePointAt(string text, int index) => FontCollection.CodePointAt(text, index);

    private static bool IsWinAnsi(int codepoint)
        => codepoint <= 0xFFFF && WinAnsiEncoding.TryGetCode((char)codepoint, out _);

    private GeneratedFont ResolveSfnt(SfntFont sfnt)
    {
        if (sfntFonts.TryGetValue(sfnt, out var existing))
        {
            return existing;
        }

        var generated = new GeneratedFont { Key = "F" + allFonts.Count.ToString(CultureInfo.InvariantCulture), Sfnt = sfnt };
        sfntFonts[sfnt] = generated;
        allFonts.Add(generated);
        return generated;
    }

    private GeneratedFont ResolveBase14(Font font)
    {
        var name = Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";
        if (base14Fonts.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var generated = new GeneratedFont { Key = "F" + allFonts.Count.ToString(CultureInfo.InvariantCulture), Base14 = name };
        base14Fonts[name] = generated;
        allFonts.Add(generated);
        return generated;
    }

    private static byte[] EncodeWinAnsi(string text)
    {
        var bytes = new List<byte>(text.Length);
        foreach (var c in text)
        {
            if (WinAnsiEncoding.TryGetCode(c, out var code))
            {
                bytes.Add(code);
            }
        }

        return [.. bytes];
    }

    private GeneratedImage Decode(Image image)
    {
        if (!images.TryGetValue(image, out var generated))
        {
            var xobject = ImageDecoder.Decode(image.Data);
            generated = new GeneratedImage
            {
                Key = "Im" + images.Count.ToString(CultureInfo.InvariantCulture),
                Image = xobject,
            };
            images[image] = generated;
        }

        return generated;
    }

    // Reverse maps for fresh (unsaved) text extraction: embedded Type0 fonts decode
    // their glyph-id codes through the accumulated gid-to-Unicode table, mirroring
    // the /ToUnicode CMap the embedder writes on save.
    private static Dictionary<string, ReverseFont> BuildExtractionFonts(GeneratedPage generated)
    {
        var map = new Dictionary<string, ReverseFont>(System.StringComparer.Ordinal);
        foreach (var font in generated.Fonts)
        {
            map[font.Key] = font.Sfnt is null ? ReverseFont.WinAnsi : ReverseFont.FromGlyphIds(font.GidToUnicode);
        }

        return map;
    }

    private static GeneratedPage Finalize(PagePlan plan)
    {
        var writer = new ContentWriter();

        foreach (var fill in plan.Fills)
        {
            writer.WriteColor(fill.Color, "rg");
            writer.WriteNumber(fill.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Y);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Width);
            writer.WriteRaw(" ");
            writer.WriteNumber(fill.Height);
            writer.WriteRaw(" re f\n");
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

        foreach (var image in plan.Images)
        {
            writer.WriteRaw("q\n");
            writer.WriteNumber(image.Width);
            writer.WriteRaw(" 0 0 ");
            writer.WriteNumber(image.Height);
            writer.WriteRaw(" ");
            writer.WriteNumber(image.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(image.Y);
            writer.WriteRaw(" cm\n");
            writer.WriteName(image.Image.Key);
            writer.WriteRaw(" Do\nQ\n");
        }

        foreach (var text in plan.Texts)
        {
            if (text.Clip is { } clip)
            {
                writer.WriteRaw("q\n");
                writer.WriteNumber(clip.X);
                writer.WriteRaw(" ");
                writer.WriteNumber(clip.Y);
                writer.WriteRaw(" ");
                writer.WriteNumber(clip.Width);
                writer.WriteRaw(" ");
                writer.WriteNumber(clip.Height);
                writer.WriteRaw(" re W n\n");
            }

            writer.WriteRaw("BT\n");
            writer.WriteColor(text.Color, "rg");
            writer.WriteName(text.Font.Key);
            writer.WriteRaw(" ");
            writer.WriteNumber(text.Size);
            writer.WriteRaw(" Tf\n");
            if (text.StrokeWidth > 0)
            {
                writer.WriteColor(text.Color, "RG");
                writer.WriteNumber(text.StrokeWidth);
                writer.WriteRaw(" w\n2 Tr\n");
            }

            if (text.Shear != 0)
            {
                writer.WriteRaw("1 0 ");
                writer.WriteNumber(text.Shear);
                writer.WriteRaw(" 1 ");
                writer.WriteNumber(text.X);
                writer.WriteRaw(" ");
                writer.WriteNumber(text.Baseline);
                writer.WriteRaw(" Tm\n");
            }
            else
            {
                writer.WriteNumber(text.X);
                writer.WriteRaw(" ");
                writer.WriteNumber(text.Baseline);
                writer.WriteRaw(" Td\n");
            }
            writer.WriteString(text.Bytes);
            writer.WriteRaw(" Tj\n");
            if (text.StrokeWidth > 0)
            {
                writer.WriteRaw("0 Tr\n");
            }

            writer.WriteRaw("ET\n");
            if (text.Clip is not null)
            {
                writer.WriteRaw("Q\n");
            }
        }

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

}

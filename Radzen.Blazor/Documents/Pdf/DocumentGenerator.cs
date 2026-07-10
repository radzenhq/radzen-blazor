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

internal sealed class GeneratedPage
{
    public required byte[] Content { get; init; }

    public required IReadOnlyList<GeneratedFont> Fonts { get; init; }

    public required IReadOnlyList<GeneratedImage> Images { get; init; }
}

// Runs the merged layout engine (Paginator for paragraph flow, TableLayout +
// TablePaginator for tables) over a DocumentBuilder and emits each laid-out page as a
// physical Page whose content stream is written directly - positioned text, painted
// images and stroked cell borders - never materializing ContentElement objects.
internal sealed class DocumentGenerator
{
    private sealed class TextDraw
    {
        public required double X { get; init; }
        public required double Baseline { get; init; }
        public required double Size { get; init; }
        public required Color Color { get; init; }
        public required GeneratedFont Font { get; init; }
        public required byte[] Bytes { get; init; }
    }

    private sealed class ImageDraw
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required GeneratedImage Image { get; init; }
    }

    private sealed class FillDraw
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required Color Color { get; init; }
    }

    private sealed class EdgeDraw
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
        public HashSet<GeneratedFont> UsedFonts { get; } = [];
        public HashSet<GeneratedImage> UsedImages { get; } = [];
    }

    private readonly DocumentBuilder builder;
    private readonly FontCollection fonts;
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

        var plans = new List<PagePlan>();
        foreach (var section in builder.Sections)
        {
            GenerateSection(section, plans);
        }

        foreach (var plan in plans)
        {
            var generated = Finalize(plan);
            var page = new Page(plan.Size.Width, plan.Size.Height)
            {
                Generated = generated,
            };
            page.SetContent(generated.Content);
            document.Pages.Insert(document.Pages.Count, page);
        }

        return document;
    }

    private void GenerateSection(Section section, List<PagePlan> plans)
    {
        foreach (var page in Paginator.Paginate(section, fonts, MeasureImage))
        {
            var height = page.Size.Height.Point;
            var plan = new PagePlan { Size = page.Size };
            var left = page.ContentBox.X;
            var contentTop = height - page.ContentBox.Y;

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

            foreach (var line in page.Header)
            {
                EmitLine(plan, line.Line, left, height - line.Y);
            }

            foreach (var positioned in page.HeaderImages)
            {
                EmitImage(plan, positioned, left, height);
            }

            var bandTop = height - (page.ContentBox.Y + page.ContentBox.Height);
            foreach (var line in page.Footer)
            {
                EmitLine(plan, line.Line, left, bandTop - line.Y);
            }

            foreach (var positioned in page.FooterImages)
            {
                EmitImage(plan, positioned, left, bandTop);
            }

            plans.Add(plan);
        }
    }

    private (double Width, double Height) MeasureImage(Image image)
    {
        var (width, height, _) = Decode(image);
        return (width, height);
    }

    private void EmitImage(PagePlan plan, PositionedImage positioned, double left, double top)
    {
        var (_, _, xobject) = Decode(positioned.Source);
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
        foreach (var row in positioned.Fragment.Rows)
        {
            if (layout.Source?.Rows[row.SourceRow].Background is { } background)
            {
                plan.Fills.Add(new FillDraw
                {
                    X = left,
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
                EmitCell(plan, layout, cell, left, contentTop, delta);
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

        foreach (var line in cell.Lines)
        {
            EmitLine(plan, line.Line, left + line.X, contentTop - (line.Y + delta));
        }

        foreach (var image in cell.Images)
        {
            var (_, _, xobject) = Decode(image.Source);
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
            foreach (var nestedCell in nested.Layout.Cells)
            {
                EmitCell(plan, nested.Layout, nestedCell, left + nested.X, contentTop, delta + nested.Y);
            }
        }
    }

    private static void EmitBorders(PagePlan plan, LaidOutTable layout, LaidOutCell cell, double left, double contentTop, double delta)
    {
        var cellBorders = cell.Cell.Borders;
        var tableBorders = layout.Source?.Borders;

        var x = left + cell.Bounds.X;
        var top = contentTop - (cell.Bounds.Y + delta);
        var right = x + cell.Bounds.Width;
        var bottom = top - cell.Bounds.Height;

        EmitEdge(plan, cellBorders.Top, tableBorders?.Top, x, top, right, top);
        EmitEdge(plan, cellBorders.Right, tableBorders?.Right, right, bottom, right, top);
        EmitEdge(plan, cellBorders.Bottom, tableBorders?.Bottom, x, bottom, right, bottom);
        EmitEdge(plan, cellBorders.Left, tableBorders?.Left, x, bottom, x, top);
    }

    private static void EmitEdge(PagePlan plan, Border cellEdge, Border? tableEdge, double x1, double y1, double x2, double y2)
    {
        var edge = cellEdge.IsSet || tableEdge is null ? cellEdge : tableEdge;
        if (edge.Style == BorderStyle.None)
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
            Style = edge.Style,
        });
    }

    private void EmitLine(PagePlan plan, LineBox line, double originX, double baseline)
    {
        var y = baseline - line.Baseline;
        foreach (var fragment in line.Fragments)
        {
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
            if (fonts.TryResolveFallbackGlyph(text[i], out var face, out _) && !WinAnsiEncoding.TryGetCode(text[i], out _))
            {
                var generated = ResolveSfnt(face);
                var bytes = new List<byte>();
                var advance = 0.0;
                while (i < text.Length
                    && !WinAnsiEncoding.TryGetCode(text[i], out _)
                    && fonts.TryResolveFallbackGlyph(text[i], out var candidate, out var gid)
                    && candidate == face)
                {
                    generated.GidToUnicode[gid] = text[i];
                    bytes.Add((byte)(gid >> 8));
                    bytes.Add((byte)(gid & 0xFF));
                    advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                    i++;
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
                    if (WinAnsiEncoding.TryGetCode(text[i], out _))
                    {
                        builderText.Append(text[i]);
                    }
                    else if (!fonts.TryResolveFallbackGlyph(text[i], out _, out _))
                    {
                        builderText.Append('?');
                    }
                    else
                    {
                        break;
                    }

                    i++;
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
            var bytes = new List<byte>();
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
            });

            runX += advance;
        }
    }

    // A lone surrogate yields its own code unit so it maps through the same
    // gid-to-unicode path without throwing.
    private static int CodePointAt(string text, int index)
        => char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1])
            ? char.ConvertToUtf32(text[index], text[index + 1])
            : text[index];

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

    private (double Width, double Height, GeneratedImage Image) Decode(Image image)
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

        var (width, height) = ImageDecoder.Measure(image, generated.Image);
        return (width, height, generated);
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
            writer.WriteRaw("BT\n");
            writer.WriteColor(text.Color, "rg");
            writer.WriteName(text.Font.Key);
            writer.WriteRaw(" ");
            writer.WriteNumber(text.Size);
            writer.WriteRaw(" Tf\n");
            writer.WriteNumber(text.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(text.Baseline);
            writer.WriteRaw(" Td\n");
            writer.WriteString(text.Bytes);
            writer.WriteRaw(" Tj\nET\n");
        }

        var usedFonts = new List<GeneratedFont>(plan.UsedFonts);
        var usedImages = new List<GeneratedImage>(plan.UsedImages);
        return new GeneratedPage
        {
            Content = writer.ToArray(),
            Fonts = usedFonts,
            Images = usedImages,
        };
    }

}

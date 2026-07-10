#nullable enable
using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;

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

    private sealed class RectDraw
    {
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double Width { get; init; }
        public required double Height { get; init; }
        public required double LineWidth { get; init; }
        public required Color Color { get; init; }
    }

    private sealed class PagePlan
    {
        public required PageSize Size { get; init; }
        public List<RectDraw> Rects { get; } = [];
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
            var page = new Page(plan.Size.Width, plan.Size.Height)
            {
                Generated = Finalize(plan),
            };
            document.Pages.Insert(document.Pages.Count, page);
        }

        return document;
    }

    private void GenerateSection(Section section, List<PagePlan> plans)
    {
        if (HasFlowOnly(section))
        {
            GenerateFlowSection(section, plans);
        }
        else
        {
            GenerateMixedSection(section, plans);
        }
    }

    private static bool HasFlowOnly(Section section)
    {
        foreach (var block in section.Blocks)
        {
            if (block is not Paragraph && block is not PageBreak)
            {
                return false;
            }
        }

        return true;
    }

    private void GenerateFlowSection(Section section, List<PagePlan> plans)
    {
        foreach (var page in Paginator.Paginate(section, fonts))
        {
            var height = page.Size.Height.Point;
            var plan = new PagePlan { Size = page.Size };

            var contentTop = height - page.ContentBox.Y;
            foreach (var line in page.Lines)
            {
                EmitLine(plan, line.Line, page.ContentBox.X, contentTop - line.Y);
            }

            foreach (var line in page.Header)
            {
                EmitLine(plan, line.Line, page.ContentBox.X, height - line.Y);
            }

            var bandTop = height - (page.ContentBox.Y + page.ContentBox.Height);
            foreach (var line in page.Footer)
            {
                EmitLine(plan, line.Line, page.ContentBox.X, bandTop - line.Y);
            }

            plans.Add(plan);
        }
    }

    private void GenerateMixedSection(Section section, List<PagePlan> plans)
    {
        var (pageWidth, pageHeight) = EffectiveSize(section);
        var left = section.Margins.Left.Point;
        var top = section.Margins.Top.Point;
        var right = section.Margins.Right.Point;
        var bottom = section.Margins.Bottom.Point;
        var contentWidth = pageWidth - left - right;
        var contentHeight = pageHeight - top - bottom;
        var size = new PageSize(Unit.FromPoint(pageWidth), Unit.FromPoint(pageHeight));
        var contentTop = pageHeight - top;

        PagePlan plan = new() { Size = size };
        plans.Add(plan);
        var cursor = 0.0;

        PagePlan NewPage()
        {
            plan = new PagePlan { Size = size };
            plans.Add(plan);
            cursor = 0;
            return plan;
        }

        foreach (var block in section.Blocks)
        {
            switch (block)
            {
                case PageBreak:
                    NewPage();
                    break;

                case Paragraph paragraph:
                    cursor += paragraph.SpacingBefore.Point;
                    foreach (var line in LineBreaker.Break(paragraph, contentWidth, fonts))
                    {
                        if (cursor + line.Height > contentHeight + 1e-6 && HasContent(plan))
                        {
                            NewPage();
                        }

                        EmitLine(plan, line, left, contentTop - cursor);
                        cursor += line.Height;
                    }

                    cursor += paragraph.SpacingAfter.Point;
                    break;

                case Image image:
                    var (iw, ih, xobject) = Decode(image);
                    if (cursor + ih > contentHeight + 1e-6 && HasContent(plan))
                    {
                        NewPage();
                    }

                    plan.Images.Add(new ImageDraw
                    {
                        X = left,
                        Y = contentTop - cursor - ih,
                        Width = iw,
                        Height = ih,
                        Image = xobject,
                    });
                    plan.UsedImages.Add(xobject);
                    cursor += ih;
                    break;

                case Table table:
                    if (HasContent(plan))
                    {
                        NewPage();
                    }

                    EmitTable(table, left, contentTop, contentWidth, contentHeight, plans, ref plan, out cursor);
                    break;
            }
        }
    }

    private void EmitTable(
        Table table,
        double left,
        double contentTop,
        double contentWidth,
        double contentHeight,
        List<PagePlan> plans,
        ref PagePlan plan,
        out double cursor)
    {
        var layout = TableLayout.Layout(table, contentWidth, fonts);
        var fragments = TablePaginator.Paginate(layout, table, contentHeight);
        var cellsByRow = new Dictionary<int, List<LaidOutCell>>();
        foreach (var cell in layout.Cells)
        {
            if (!cellsByRow.TryGetValue(cell.Row, out var list))
            {
                list = [];
                cellsByRow[cell.Row] = list;
            }

            list.Add(cell);
        }

        var fragmentHeight = 0.0;
        for (var f = 0; f < fragments.Count; f++)
        {
            if (f > 0)
            {
                plan = new PagePlan { Size = plan.Size };
                plans.Add(plan);
            }

            var fragment = fragments[f];
            fragmentHeight = fragment.Height;
            foreach (var row in fragment.Rows)
            {
                if (!cellsByRow.TryGetValue(row.SourceRow, out var cells))
                {
                    continue;
                }

                foreach (var cell in cells)
                {
                    var delta = row.Y - cell.Bounds.Y;
                    EmitCell(plan, cell, left, contentTop, delta);
                }
            }
        }

        cursor = fragmentHeight;
    }

    private void EmitCell(PagePlan plan, LaidOutCell cell, double left, double contentTop, double delta)
    {
        var bounds = cell.Bounds;
        EmitBorders(plan, cell, left, contentTop, delta);

        foreach (var line in cell.Lines)
        {
            EmitLine(plan, line.Line, left + line.X, contentTop - (line.Y + delta));
        }

        _ = bounds;
    }

    private static void EmitBorders(PagePlan plan, LaidOutCell cell, double left, double contentTop, double delta)
    {
        var borders = cell.Cell.Borders;
        var visible = borders.Top.Style != BorderStyle.None
            || borders.Right.Style != BorderStyle.None
            || borders.Bottom.Style != BorderStyle.None
            || borders.Left.Style != BorderStyle.None;
        if (!visible)
        {
            return;
        }

        var x = left + cell.Bounds.X;
        var topEdge = contentTop - (cell.Bounds.Y + delta);
        var width = cell.Bounds.Width;
        var height = cell.Bounds.Height;
        var lineWidth = borders.Top.Width > 0 ? borders.Top.Width : 0.5;

        plan.Rects.Add(new RectDraw
        {
            X = x,
            Y = topEdge - height,
            Width = width,
            Height = height,
            LineWidth = lineWidth,
            Color = borders.Top.Color,
        });
    }

    private void EmitLine(PagePlan plan, LineBox line, double originX, double baseline)
    {
        foreach (var fragment in line.Fragments)
        {
            var text = fragment.Text;
            if (text.Length == 0)
            {
                continue;
            }

            var font = fragment.Run.Font;
            var y = baseline - line.Baseline;
            if (fonts.TryResolvePrimary(font, out var primary))
            {
                EmitSfntFragment(plan, fragment, primary, originX + fragment.XOffset, y);
            }
            else
            {
                var generated = ResolveBase14(font);
                var bytes = EncodeWinAnsi(text);
                if (bytes.Length == 0)
                {
                    continue;
                }

                plan.UsedFonts.Add(generated);
                plan.Texts.Add(new TextDraw
                {
                    X = originX + fragment.XOffset,
                    Baseline = y,
                    Size = font.Size,
                    Color = font.Color,
                    Font = generated,
                    Bytes = bytes,
                });
            }
        }
    }

    // Splits a fragment into maximal sub-runs by the physical face that actually
    // supplies each glyph (primary or a SetFallback face), so every glyph is drawn
    // by the embedded subset that owns it - not the primary's .notdef.
    private void EmitSfntFragment(PagePlan plan, LineFragment fragment, SfntFont primary, double startX, double y)
    {
        var font = fragment.Run.Font;
        var size = font.Size;
        var text = fragment.Text;
        var runX = startX;

        var i = 0;
        while (i < text.Length)
        {
            var (face, _) = fonts.ResolveGlyph(primary, text[i]);
            var generated = ResolveSfnt(face);
            var bytes = new List<byte>();
            var advance = 0.0;
            while (i < text.Length)
            {
                var (candidate, gid) = fonts.ResolveGlyph(primary, text[i]);
                if (candidate != face)
                {
                    break;
                }

                generated.GidToUnicode[gid] = text[i];
                bytes.Add((byte)(gid >> 8));
                bytes.Add((byte)(gid & 0xFF));
                advance += face.GetAdvanceWidth(gid) * size / face.UnitsPerEm;
                i++;
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

        var dict = generated.Image.Image.Dictionary;
        var pixelWidth = ((NumberObject)dict["Width"]).DoubleValue;
        var pixelHeight = ((NumberObject)dict["Height"]).DoubleValue;

        double width;
        double height;
        if (image.Width is { } w && image.Height is { } h)
        {
            width = w.Point;
            height = h.Point;
        }
        else if (image.Width is { } wo)
        {
            width = wo.Point;
            height = pixelHeight * width / pixelWidth;
        }
        else if (image.Height is { } ho)
        {
            height = ho.Point;
            width = pixelWidth * height / pixelHeight;
        }
        else
        {
            width = pixelWidth;
            height = pixelHeight;
        }

        return (width, height, generated);
    }

    private static GeneratedPage Finalize(PagePlan plan)
    {
        var writer = new ContentWriter();

        foreach (var rect in plan.Rects)
        {
            writer.WriteColor(rect.Color, "RG");
            writer.WriteNumber(rect.LineWidth);
            writer.WriteRaw(" w\n");
            writer.WriteNumber(rect.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(rect.Y);
            writer.WriteRaw(" ");
            writer.WriteNumber(rect.Width);
            writer.WriteRaw(" ");
            writer.WriteNumber(rect.Height);
            writer.WriteRaw(" re S\n");
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

    private static bool HasContent(PagePlan plan)
        => plan.Texts.Count > 0 || plan.Images.Count > 0 || plan.Rects.Count > 0;

    private static (double Width, double Height) EffectiveSize(Section section)
    {
        var width = section.PageSize.Width.Point;
        var height = section.PageSize.Height.Point;
        return section.Orientation == PageOrientation.Landscape ? (height, width) : (width, height);
    }
}

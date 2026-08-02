#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class InvoiceStylingRegressionTests
{
    private const double Tol = 0.5;

    private static byte[] PageBytes(Document document)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);

    private static List<ContentOperation> Ops(Document document)
        => ContentStreamTokenizer.Parse(PageBytes(document));

    private static List<(string Text, double X, double Y)> TextRuns(Document document)
    {
        var content = Encoding.Latin1.GetString(PageBytes(document));
        var runs = new List<(string, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value,
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static List<(double X1, double Y1, double X2, double Y2)> Segments(List<ContentOperation> ops)
    {
        var segments = new List<(double, double, double, double)>();
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "m" && ops[i + 1].Operator == "l"
                && ops[i].Operands.Count >= 2 && ops[i + 1].Operands.Count >= 2)
            {
                segments.Add((ops[i].Num(0), ops[i].Num(1), ops[i + 1].Num(0), ops[i + 1].Num(1)));
            }
        }

        return segments;
    }

    private static bool IsHorizontal((double X1, double Y1, double X2, double Y2) s)
        => Math.Abs(s.Y1 - s.Y2) < 0.001 && Math.Abs(s.X1 - s.X2) > 1;

    private static void SetUnitProperty(object target, string name, double points)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, $"{target.GetType().Name}.{name} public property is missing");
        var type = Nullable.GetUnderlyingType(property!.PropertyType) ?? property.PropertyType;
        Assert.True(type == typeof(Unit), $"{target.GetType().Name}.{name} must be a Unit, was {property.PropertyType}");
        property.SetValue(target, Unit.FromPoint(points));
    }

    private static (Document Builder, Section Section) NewDocument()
    {
        var document = new Document();
        return (document, document.Sections.Add());
    }

    private static Table OneCellTable(Section section, string text, double columnWidth = 200)
    {
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(columnWidth));
        var row = table.Rows.Add();
        row.Cells[0].Blocks.AddParagraph(text);
        return table;
    }

    [Fact]
    public void CellBorderWidthWithoutStyle_RendersSolidBottomRule()
    {
        var (document, section) = NewDocument();
        var table = OneCellTable(section, "X");
        table.Rows[0].Cells[0].Borders.Bottom.Width = 0.5;

        var ops = Ops(document);
        var horizontal = Segments(ops).Where(IsHorizontal).ToList();

        Assert.True(horizontal.Count > 0, "a border edge with Width > 0 and default Style must stroke a line");
        Assert.Contains(ops, op => op.Operator == "S");
        Assert.Contains(ops, op => op.Operator == "w" && op.Operands.Count == 1 && Math.Abs(op.Num(0) - 0.5) < 0.005);
    }

    [Fact]
    public void TableBorderWidthWithoutStyle_RendersGrid()
    {
        var (document, section) = NewDocument();
        var table = OneCellTable(section, "X");
        table.Borders.SetAll(width: 1);

        var segments = Segments(Ops(document));

        Assert.True(segments.Any(IsHorizontal), "table borders with Width > 0 must draw horizontal edges");
        Assert.True(
            segments.Any(s => Math.Abs(s.X1 - s.X2) < 0.001 && Math.Abs(s.Y1 - s.Y2) > 1),
            "table borders with Width > 0 must draw vertical edges");
    }

    [Fact]
    public void EmptyParagraph_AdvancesFlowByOneLine()
    {
        static double Gap(bool withSpacer)
        {
            var (document, section) = NewDocument();
            section.Blocks.AddParagraph("First");
            if (withSpacer)
            {
                section.Blocks.AddParagraph();
            }

            section.Blocks.AddParagraph("Second");

            var runs = TextRuns(document);
            var first = runs.Single(r => r.Text == "First");
            var second = runs.Single(r => r.Text == "Second");
            return first.Y - second.Y;
        }

        var extra = Gap(withSpacer: true) - Gap(withSpacer: false);

        Assert.True(extra >= 8, $"an empty paragraph must occupy one line of its font height, added only {extra:0.##}pt");
    }

    [Fact]
    public void Tab_AdvancesToNextDefaultTabStop()
    {
        var (document, section) = NewDocument();
        section.Blocks.AddParagraph("ID:\tAAA");
        section.Blocks.AddParagraph("No:\tBBB");

        var runs = TextRuns(document);
        var label = runs.SingleOrDefault(r => r.Text == "ID:");
        var a = runs.SingleOrDefault(r => r.Text == "AAA");
        var b = runs.SingleOrDefault(r => r.Text == "BBB");

        Assert.True(a.Text == "AAA" && b.Text == "BBB" && label.Text == "ID:",
            "runs before and after a tab must be emitted as separately positioned fragments");

        Assert.True(Math.Abs(a.X - b.X) <= Tol,
            $"values after a tab must align on the same tab stop (got {a.X:0.##} vs {b.X:0.##})");

        var advance = a.X - label.X;
        Assert.True(advance >= 30 && advance <= 80,
            $"a tab after a short label must advance to the first default tab stop, advanced {advance:0.##}pt");
    }

    [Fact]
    public void TableLeftIndent_ShiftsColumnOrigin()
    {
        static double CellX(double indent)
        {
            var (document, section) = NewDocument();
            var table = OneCellTable(section, "CELL", 150);
            if (indent > 0)
            {
                SetUnitProperty(table, "LeftIndent", indent);
            }

            return TextRuns(document).Single(r => r.Text == "CELL").X;
        }

        var shift = CellX(200) - CellX(0);
        Assert.True(Math.Abs(shift - 200) <= 1,
            $"Table.LeftIndent = 200pt must shift cell content by 200pt, shifted {shift:0.##}pt");
    }

    [Fact]
    public void RowBorders_BottomRule_SpansAllRowCells()
    {
        var (document, section) = NewDocument();
        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(100));
        table.Columns.Add(Unit.FromPoint(100));
        var row = table.Rows.Add();
        row.Cells[0].Blocks.AddParagraph("A");
        row.Cells[1].Blocks.AddParagraph("B");

        var property = typeof(Row).GetProperty("Borders", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, "Row.Borders public property is missing");
        var borders = Assert.IsType<Borders>(property!.GetValue(row));
        borders.Bottom.Width = 0.5;
        borders.Bottom.Style = BorderStyle.Solid;

        var horizontal = Segments(Ops(document)).Where(IsHorizontal).ToList();
        Assert.True(horizontal.Count > 0, "a row-level bottom border must stroke horizontal rules on its cells");

        var bottom = horizontal.MinBy(s => s.Y1);
        var atBottom = horizontal.Where(s => Math.Abs(s.Y1 - bottom.Y1) < 0.01).ToList();
        var span = atBottom.Max(s => Math.Max(s.X1, s.X2)) - atBottom.Min(s => Math.Min(s.X1, s.X2));
        Assert.True(span >= 190, $"the row bottom rule must span both cells (~200pt), spanned {span:0.##}pt");
    }

    [Fact]
    public void ParagraphLeftIndent_ShiftsLineOrigin()
    {
        static double TextX(double indent)
        {
            var (document, section) = NewDocument();
            var paragraph = section.Blocks.AddParagraph("Bullet");
            if (indent > 0)
            {
                SetUnitProperty(paragraph, "LeftIndent", indent);
            }

            return TextRuns(document).Single(r => r.Text == "Bullet").X;
        }

        var shift = TextX(50) - TextX(0);
        Assert.True(Math.Abs(shift - 50) <= Tol,
            $"Paragraph.LeftIndent = 50pt must shift the line origin by 50pt, shifted {shift:0.##}pt");
    }

    [Fact]
    public void ImageWithoutExplicitSize_FitsContentWidth()
    {
        var (document, section) = NewDocument();
        section.Blocks.AddImage(new MemoryStream(GrayPng(2000, 400)));

        var contentWidth = section.PageSize.Width.Point - section.Margins.Left.Point - section.Margins.Right.Point;
        var (width, height) = ImagePlacement(Ops(document));

        Assert.True(width <= contentWidth + Tol,
            $"an unsized 2000px image must be clamped to the {contentWidth:0.##}pt content width, drew {width:0.##}pt");
        Assert.True(width > 50, $"clamped image width must stay usable, drew {width:0.##}pt");
        Assert.True(Math.Abs(height / width - 400.0 / 2000.0) < 0.05 * (400.0 / 2000.0),
            $"clamping must preserve the aspect ratio, drew {width:0.##}x{height:0.##}pt");
    }

    [Fact]
    public void ImageWithExplicitSize_UsesGivenSize()
    {
        var (document, section) = NewDocument();
        var image = section.Blocks.AddImage(new MemoryStream(GrayPng(2000, 400)));
        image.Width = Unit.FromPoint(120);
        image.Height = Unit.FromPoint(48);

        var (width, height) = ImagePlacement(Ops(document));

        Assert.Equal(120, width, 1);
        Assert.Equal(48, height, 1);
    }

    private static (double Width, double Height) ImagePlacement(List<ContentOperation> ops)
    {
        for (var i = 0; i + 1 < ops.Count; i++)
        {
            if (ops[i].Operator == "cm" && ops[i].Operands.Count == 6 && ops[i + 1].Operator == "Do")
            {
                return (ops[i].Num(0), ops[i].Num(3));
            }
        }

        Assert.Fail("no image cm/Do placement found in the page content");
        return default;
    }

    private static byte[] GrayPng(int width, int height)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)height);
        ihdr[8] = 8;
        ihdr[9] = 0;
        WriteChunk(ms, "IHDR", ihdr);

        var raw = new byte[(width + 1) * height];
        using (var idat = new MemoryStream())
        {
            using (var zlib = new ZLibStream(idat, CompressionMode.Compress, leaveOpen: true))
            {
                zlib.Write(raw, 0, raw.Length);
            }

            WriteChunk(ms, "IDAT", idat.ToArray());
        }

        WriteChunk(ms, "IEND", []);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(word, (uint)data.Length);
        stream.Write(word);
        stream.Write(typeBytes);
        stream.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(word, Crc32(typeBytes, data));
        stream.Write(word);
    }

    private static uint Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var chunk in new[] { type, data })
        {
            foreach (var b in chunk)
            {
                crc ^= b;
                for (var i = 0; i < 8; i++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }
        }

        return crc ^ 0xFFFFFFFF;
    }
}

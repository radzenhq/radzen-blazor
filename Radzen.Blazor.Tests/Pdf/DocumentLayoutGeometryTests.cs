#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System;
using Radzen.Documents.Codes;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

internal static class LayoutGeometry
{
    public static string Serialize(LaidOutDocument document)
    {
        var text = new StringBuilder();
        foreach (var page in document.Pages)
        {
            text.Append(CultureInfo.InvariantCulture, $"page {page.Number} size {N(page.Size.Width.Point)}x{N(page.Size.Height.Point)}");
            text.Append(CultureInfo.InvariantCulture, $" content {N(page.ContentBox.X)},{N(page.ContentBox.Y)},{N(page.ContentBox.Width)},{N(page.ContentBox.Height)}");
            text.AppendLine();
            Layer(text, "body", page.Body);
            Layer(text, "header", page.HeaderLayer);
            Layer(text, "footer", page.FooterLayer);
        }

        return text.ToString();
    }

    private static void Layer(StringBuilder text, string name, LaidOutLayer layer)
    {
        foreach (var line in layer.Lines)
        {
            text.AppendLine(CultureInfo.InvariantCulture,
                $"  {name} line y={N(line.Y)} w={N(line.Line.Width)} h={N(line.Line.Height)} baseline={N(line.Line.Baseline)} fragments={line.Line.Fragments.Length} source={Source(line.Source)}");
        }

        foreach (var image in layer.Images)
        {
            text.AppendLine(CultureInfo.InvariantCulture,
                $"  {name} image y={N(image.Y)} x={N(image.X)} w={N(image.Width)} h={N(image.Height)} source={Source(image.Source)}");
        }

        foreach (var code in layer.CodeSymbols)
        {
            text.AppendLine(CultureInfo.InvariantCulture,
                $"  {name} code y={N(code.Y)} x={N(code.X)} w={N(code.Width)} h={N(code.Height)} source={Source(code.Source)}");
        }

        foreach (var table in layer.Tables)
        {
            text.AppendLine(CultureInfo.InvariantCulture,
                $"  {name} table y={N(table.Bounds.Y)} w={N(table.Layout.Width)} h={N(table.Fragment.Height)} zorder={table.ZOrder} rows={table.Fragment.Rows.Length} headers={table.Fragment.HeaderRowCount} source={Source(table.Source)}");
        }

        foreach (var box in layer.Boxes)
        {
            text.AppendLine(CultureInfo.InvariantCulture,
                $"  {name} box y={N(box.Bounds.Y)} bounds={N(box.Bounds.X)},{N(box.Bounds.Y)},{N(box.Bounds.Width)},{N(box.Bounds.Height)} zorder={box.ZOrder} opacity={N(box.Opacity)} lines={box.Content.Lines.Length} source={Source(box.Source)}");
        }
    }

    public static IReadOnlyList<string> LineTexts(LaidOutLayer layer)
    {
        var texts = new List<string>();
        foreach (var line in layer.Lines)
        {
            var text = new StringBuilder();
            foreach (var fragment in line.Line.Fragments)
            {
                text.Append(fragment.Text);
            }

            texts.Add(text.ToString());
        }

        return texts;
    }

    private static string Source(SourceId source) => source.Value.ToString(CultureInfo.InvariantCulture);

    private static string N(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
}

public class DocumentLayoutGeometryTests
{
    private static Paragraph Text(string value, double size = 12)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(value);
        run.Font.Family = BuildTestSupport.Latin;
        run.Font.Size = size;
        return paragraph;
    }

    internal static Document FlowDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(220));
        section.Margins.Left = Unit.FromPoint(30);
        section.Margins.Right = Unit.FromPoint(30);
        section.Margins.Top = Unit.FromPoint(40);
        section.Margins.Bottom = Unit.FromPoint(50);
        section.Header.Blocks.Add(Text("HEADER"));
        section.Footer.Blocks.Add(Text("FOOTER"));
        for (var i = 0; i < 14; i++)
        {
            section.Blocks.Add(Text($"Body paragraph number {i} with enough words to wrap onto a second line."));
        }

        return document;
    }

    internal static Document TableDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(240));
        section.Margins.SetAll(Unit.FromPoint(20));

        var table = new Table();
        table.Columns.Add(Unit.FromPoint(180));
        table.Columns.Add(Unit.FromPoint(120));
        var header = table.Rows.Add();
        header.RepeatOnEveryPage = true;
        header.Cells[0].Blocks.Add(Text("Product"));
        header.Cells[1].Blocks.Add(Text("Amount"));
        for (var i = 0; i < 18; i++)
        {
            var row = table.Rows.Add();
            row.Cells[0].Blocks.Add(Text($"Item {i}"));
            row.Cells[1].Blocks.Add(Text($"{i * 13}.00"));
        }

        section.Blocks.Add(table);
        return document;
    }

    internal static Document GraphicsDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(25));

        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(240, 240, 250),
            CornerRadius = Unit.FromPoint(6),
            Opacity = 0.8,
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
                Spread = Unit.FromPoint(1),
            },
        });
        container.Blocks.Add(Text("Panel heading"));
        container.Blocks.Add(Text("Panel body text that is long enough to wrap inside the padded container."));

        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        section.Blocks.Add(Text("Caption under the image."));
        return document;
    }

    internal static Document CodeDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Blocks.Add(Text("Scan me"));
        section.Blocks.AddQrCode("https://www.radzen.com", Unit.FromPoint(120));
        section.Blocks.AddBarcode(BarcodeType.Code128, "RADZEN-1234", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);
        return document;
    }

    internal static Document TableOfContentsDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var front = document.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(220));
        front.Margins.SetAll(Unit.FromPoint(30));
        var toc = front.Blocks.AddTableOfContents();
        toc.Font.Family = BuildTestSupport.Latin;
        toc.Font.Size = 12;
        toc.AddEntry("Chapter One", "ch1");
        toc.AddEntry("Chapter Two", "ch2", level: 1);

        var one = document.Sections.Add();
        one.PageSize = front.PageSize;
        one.Margins.SetAll(Unit.FromPoint(30));
        one.Blocks.Add(Anchored("Chapter One body", "ch1"));

        var two = document.Sections.Add();
        two.PageSize = front.PageSize;
        two.Margins.SetAll(Unit.FromPoint(30));
        two.Blocks.Add(Text("Filler before the break"));
        two.Blocks.AddPageBreak();
        two.Blocks.Add(Anchored("Chapter Two body", "ch2"));

        return document;
    }

    private static Paragraph Anchored(string value, string anchor)
    {
        var paragraph = Text(value);
        paragraph.Inlines[0].Anchor = anchor;
        return paragraph;
    }

    [Fact]
    public void FlowDocument_Geometry_IsStable()
        => Assert.Equal(FlowGeometry, LayoutGeometry.Serialize(DocumentLayouter.Layout(FlowDocument())).TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void TableDocument_Geometry_IsStable()
        => Assert.Equal(TableGeometry, LayoutGeometry.Serialize(DocumentLayouter.Layout(TableDocument())).TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void GraphicsDocument_Geometry_IsStable()
        => Assert.Equal(GraphicsGeometry, LayoutGeometry.Serialize(DocumentLayouter.Layout(GraphicsDocument())).TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void CodeDocument_Geometry_IsStable()
        => Assert.Equal(CodeGeometry, LayoutGeometry.Serialize(DocumentLayouter.Layout(CodeDocument())).TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void TableOfContentsDocument_Geometry_IsStable()
        => Assert.Equal(TableOfContentsGeometry, LayoutGeometry.Serialize(DocumentLayouter.Layout(TableOfContentsDocument())).TrimEnd(), ignoreLineEndingDifferences: true);

    [Fact]
    public void Layout_ResolvesTableOfContentsPageNumbers()
        => Assert.Collection(
            LayoutGeometry.LineTexts(DocumentLayouter.Layout(TableOfContentsDocument()).Pages[0].Body),
            first => Assert.StartsWith("ChapterOne2", first, StringComparison.Ordinal),
            second => Assert.StartsWith("ChapterTwo4", second, StringComparison.Ordinal));

    [Fact]
    public void Layout_NumbersPagesContinuouslyAcrossSections()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.Add(Text("First"));
        document.Sections.Add().Blocks.Add(Text("Second"));

        Assert.Equal([1, 2], DocumentLayouter.Layout(document).Pages.Select(static page => page.Number));
    }

    [Fact]
    public void Layout_TableOfContentsPageNumbers_MatchGeneratedPdf()
    {
        var document = TableOfContentsDocument();

        var laidOut = LayoutGeometry.LineTexts(DocumentLayouter.Layout(document).Pages[0].Body)
            .Select(text => Regex.Match(text, @"\d+").Value)
            .ToArray();

        var rendered = Regex.Matches(BuildTestSupport.Reload(document).Pages[0].ExtractText(), @"\.{3,}\s*(\d+)")
            .Select(match => match.Groups[1].Value)
            .ToArray();

        Assert.Equal(new[] { "2", "4" }, laidOut);
        Assert.Equal(laidOut, rendered);
    }

    [Theory]
    [InlineData("flow")]
    [InlineData("table")]
    [InlineData("graphics")]
    [InlineData("code")]
    [InlineData("toc")]
    public void Layout_IsRepeatable(string kind)
    {
        var first = LayoutGeometry.Serialize(DocumentLayouter.Layout(Document(kind)));
        var second = LayoutGeometry.Serialize(DocumentLayouter.Layout(Document(kind)));

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("flow")]
    [InlineData("table")]
    [InlineData("graphics")]
    [InlineData("code")]
    [InlineData("toc")]
    public void Layout_MatchesGeneratedPages(string kind)
    {
        var document = Document(kind);
        var layout = DocumentLayouter.Layout(document);

        var generated = new DocumentRenderer().Render(document);

        Assert.Equal(layout.Pages.Length, generated.Pages.Count);
        for (var i = 0; i < generated.Pages.Count; i++)
        {
            Assert.Equal(layout.Pages[i].Size.Width.Point, generated.Pages[i].Width.Point, 6);
            Assert.Equal(layout.Pages[i].Size.Height.Point, generated.Pages[i].Height.Point, 6);
        }
    }

    [Theory]
    [InlineData("flow")]
    [InlineData("table")]
    [InlineData("graphics")]
    [InlineData("code")]
    [InlineData("toc")]
    public void Generation_StillProducesPdf(string kind)
    {
        var bytes = new DocumentRenderer().ToArray(Document(kind));

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private static Document Document(string kind) => kind switch
    {
        "flow" => FlowDocument(),
        "table" => TableDocument(),
        "graphics" => GraphicsDocument(),
        "toc" => TableOfContentsDocument(),
        _ => CodeDocument(),
    };

    private const string FlowGeometry =
        """
        page 1 size 400.00x220.00 content 30.00,49.23,340.00,120.77
          body line y=0.00 w=322.20 h=13.80 baseline=10.86 fragments=11 source=0
          body line y=13.80 w=64.04 h=13.80 baseline=10.86 fragments=2 source=0
          body line y=27.60 w=322.20 h=13.80 baseline=10.86 fragments=11 source=1
          body line y=41.40 w=64.04 h=13.80 baseline=10.86 fragments=2 source=1
          body line y=55.20 w=322.20 h=13.80 baseline=10.86 fragments=11 source=2
          body line y=68.99 w=64.04 h=13.80 baseline=10.86 fragments=2 source=2
          body line y=82.79 w=322.20 h=13.80 baseline=10.86 fragments=11 source=3
          body line y=96.59 w=64.04 h=13.80 baseline=10.86 fragments=2 source=3
          header line y=0.00 w=50.01 h=13.80 baseline=10.86 fragments=1 source=15
          footer line y=0.00 w=50.00 h=13.80 baseline=10.86 fragments=1 source=17
        page 2 size 400.00x220.00 content 30.00,49.23,340.00,120.77
          body line y=0.00 w=322.20 h=13.80 baseline=10.86 fragments=11 source=4
          body line y=13.80 w=64.04 h=13.80 baseline=10.86 fragments=2 source=4
          body line y=27.60 w=322.20 h=13.80 baseline=10.86 fragments=11 source=5
          body line y=41.40 w=64.04 h=13.80 baseline=10.86 fragments=2 source=5
          body line y=55.20 w=322.20 h=13.80 baseline=10.86 fragments=11 source=6
          body line y=68.99 w=64.04 h=13.80 baseline=10.86 fragments=2 source=6
          body line y=82.79 w=322.20 h=13.80 baseline=10.86 fragments=11 source=7
          body line y=96.59 w=64.04 h=13.80 baseline=10.86 fragments=2 source=7
          header line y=0.00 w=50.01 h=13.80 baseline=10.86 fragments=1 source=15
          footer line y=0.00 w=50.00 h=13.80 baseline=10.86 fragments=1 source=17
        page 3 size 400.00x220.00 content 30.00,49.23,340.00,120.77
          body line y=0.00 w=322.20 h=13.80 baseline=10.86 fragments=11 source=8
          body line y=13.80 w=64.04 h=13.80 baseline=10.86 fragments=2 source=8
          body line y=27.60 w=322.20 h=13.80 baseline=10.86 fragments=11 source=9
          body line y=41.40 w=64.04 h=13.80 baseline=10.86 fragments=2 source=9
          body line y=55.20 w=328.88 h=13.80 baseline=10.86 fragments=11 source=10
          body line y=68.99 w=64.04 h=13.80 baseline=10.86 fragments=2 source=10
          body line y=82.79 w=328.88 h=13.80 baseline=10.86 fragments=11 source=11
          body line y=96.59 w=64.04 h=13.80 baseline=10.86 fragments=2 source=11
          header line y=0.00 w=50.01 h=13.80 baseline=10.86 fragments=1 source=15
          footer line y=0.00 w=50.00 h=13.80 baseline=10.86 fragments=1 source=17
        page 4 size 400.00x220.00 content 30.00,49.23,340.00,120.77
          body line y=0.00 w=328.88 h=13.80 baseline=10.86 fragments=11 source=12
          body line y=13.80 w=64.04 h=13.80 baseline=10.86 fragments=2 source=12
          body line y=27.60 w=328.88 h=13.80 baseline=10.86 fragments=11 source=13
          body line y=41.40 w=64.04 h=13.80 baseline=10.86 fragments=2 source=13
          header line y=0.00 w=50.01 h=13.80 baseline=10.86 fragments=1 source=15
          footer line y=0.00 w=50.00 h=13.80 baseline=10.86 fragments=1 source=17
        """;

    private const string TableGeometry =
        """
        page 1 size 400.00x240.00 content 20.00,20.00,360.00,200.00
          body table y=0.00 w=300.00 h=193.18 zorder=0 rows=14 headers=1 source=114
        page 2 size 400.00x240.00 content 20.00,20.00,360.00,200.00
          body table y=0.00 w=300.00 h=82.79 zorder=0 rows=6 headers=1 source=114
        """;

    private const string GraphicsGeometry =
        """
        page 1 size 400.00x500.00 content 25.00,25.00,350.00,450.00
          body line y=109.40 w=134.75 h=13.80 baseline=10.86 fragments=4 source=3
          body image y=61.40 x=0.00 w=48.00 h=48.00 source=2
          body box y=0.00 bounds=0.00,0.00,350.00,61.40 zorder=0 opacity=0.80 lines=3 source=7
        """;

    private const string TableOfContentsGeometry =
        """
        page 1 size 400.00x220.00 content 30.00,30.00,340.00,160.00
          body line y=0.00 w=311.30 h=13.80 baseline=10.86 fragments=4 source=9
          body line y=13.80 w=299.30 h=13.80 baseline=10.86 fragments=4 source=10
        page 2 size 400.00x220.00 content 30.00,30.00,340.00,160.00
          body line y=0.00 w=98.06 h=13.80 baseline=10.86 fragments=3 source=0
        page 3 size 400.00x220.00 content 30.00,30.00,340.00,160.00
          body line y=0.00 w=116.72 h=13.80 baseline=10.86 fragments=4 source=1
        page 4 size 400.00x220.00 content 30.00,30.00,340.00,160.00
          body line y=0.00 w=98.05 h=13.80 baseline=10.86 fragments=3 source=2
        """;

    private const string CodeGeometry =
        """
        page 1 size 400.00x400.00 content 20.00,20.00,360.00,360.00
          body line y=0.00 w=47.36 h=13.80 baseline=10.86 fragments=2 source=0
          body code y=13.80 x=0.00 w=120.00 h=120.00 source=1
          body code y=133.80 x=0.00 w=200.00 h=52.00 source=2
        """;
}

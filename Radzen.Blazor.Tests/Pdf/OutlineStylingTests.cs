#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class OutlineStylingTests
{
    private static Document ThreePages()
    {
        var document = new Document();
        for (var i = 0; i < 3; i++)
        {
            var section = document.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margins.SetAll(Unit.FromPoint(40));
            section.Blocks.Add(new Paragraph()).Inlines.Add("Page " + i);
        }

        return document;
    }

    private static DictionaryObject Resolve(DocumentReader reader, DocumentObject value)
        => Assert.IsType<DictionaryObject>(reader.Resolve(value));

    private static DictionaryObject Outlines(DocumentReader reader)
        => Resolve(reader, ContentTestHelpers.Catalog(reader)["Outlines"]);

    [Fact]
    public void OutlineItem_EmitsColorAndStyleFlags()
    {
        var document = ThreePages();
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(new OutlineItem("Styled", OutlineTarget.ToPage(0))
        {
            Color = Color.Red,
            Bold = true,
            Italic = true,
        });

        var reader = DocumentReader.Parse(rendered.ToArray());
        var item = Resolve(reader, Outlines(reader)["First"]);

        var color = Assert.IsType<ArrayObject>(reader.Resolve(item["C"]));
        Assert.Equal(3, color.Count);
        Assert.Equal(1.0, Assert.IsType<NumberObject>(reader.Resolve(color[0])).DoubleValue, 5);
        Assert.Equal(0.0, Assert.IsType<NumberObject>(reader.Resolve(color[1])).DoubleValue, 5);
        Assert.Equal(0.0, Assert.IsType<NumberObject>(reader.Resolve(color[2])).DoubleValue, 5);

        Assert.Equal(3, Assert.IsType<NumberObject>(reader.Resolve(item["F"])).IntValue);
    }

    [Fact]
    public void CollapsedItem_EmitsNegativeCount_AndHidesDescendantsFromAncestorCount()
    {
        var document = ThreePages();
        var parent = new OutlineItem("Parent", OutlineTarget.ToPage(0)) { Collapsed = true };
        parent.Children.Add(new OutlineItem("Child A", OutlineTarget.ToPage(1)));
        parent.Children.Add(new OutlineItem("Child B", OutlineTarget.ToPage(2)));
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(parent);

        var reader = DocumentReader.Parse(rendered.ToArray());
        var outlines = Outlines(reader);

        Assert.Equal(1, Assert.IsType<NumberObject>(reader.Resolve(outlines["Count"])).IntValue);

        var parentNode = Resolve(reader, outlines["First"]);
        Assert.Equal(-2, Assert.IsType<NumberObject>(reader.Resolve(parentNode["Count"])).IntValue);
    }

    [Fact]
    public void OpenParent_KeepsPositiveCounts()
    {
        var document = ThreePages();
        var rendered = new DocumentRenderer().Render(document);
        var parent = new OutlineItem("Parent", OutlineTarget.ToPage(0));
        parent.Children.Add(new OutlineItem("Child A", OutlineTarget.ToPage(1)));
        parent.Children.Add(new OutlineItem("Child B", OutlineTarget.ToPage(2)));
        rendered.Outline.Add(parent);

        var reader = DocumentReader.Parse(rendered.ToArray());
        var outlines = Outlines(reader);
        Assert.Equal(3, Assert.IsType<NumberObject>(reader.Resolve(outlines["Count"])).IntValue);
        Assert.Equal(2, Assert.IsType<NumberObject>(reader.Resolve(Resolve(reader, outlines["First"])["Count"])).IntValue);
    }

    private static ArrayObject Dest(DocumentReader reader, OutlineTarget target)
    {
        var document = ThreePages();
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(new OutlineItem("D", target));
        var read = DocumentReader.Parse(rendered.ToArray());
        return Assert.IsType<ArrayObject>(read.Resolve(Resolve(read, Outlines(read)["First"])["Dest"]));
    }

    [Fact]
    public void FitModes_EmitCorrectDestinationArrays()
    {
        var reader = BuildTestSupport.Read(ThreePages());

        var fit = Dest(reader, OutlineTarget.ToPageFit(1));
        Assert.Equal(2, fit.Count);
        Assert.Equal("Fit", Assert.IsType<NameObject>(reader.Resolve(fit[1])).Value);

        var fitH = Dest(reader, OutlineTarget.ToPageFitHorizontal(1, 250));
        Assert.Equal(3, fitH.Count);
        Assert.Equal("FitH", Assert.IsType<NameObject>(reader.Resolve(fitH[1])).Value);
        Assert.Equal(250, Assert.IsType<NumberObject>(reader.Resolve(fitH[2])).DoubleValue, 5);

        var fitR = Dest(reader, OutlineTarget.ToPageRectangle(1, 10, 20, 100, 200));
        Assert.Equal(6, fitR.Count);
        Assert.Equal("FitR", Assert.IsType<NameObject>(reader.Resolve(fitR[1])).Value);
        Assert.Equal(10, Assert.IsType<NumberObject>(reader.Resolve(fitR[2])).DoubleValue, 5);
        Assert.Equal(20, Assert.IsType<NumberObject>(reader.Resolve(fitR[3])).DoubleValue, 5);
        Assert.Equal(100, Assert.IsType<NumberObject>(reader.Resolve(fitR[4])).DoubleValue, 5);
        Assert.Equal(200, Assert.IsType<NumberObject>(reader.Resolve(fitR[5])).DoubleValue, 5);

        var xyz = Dest(reader, OutlineTarget.ToPageXYZ(1, 5, 295, 2.0));
        Assert.Equal(5, xyz.Count);
        Assert.Equal("XYZ", Assert.IsType<NameObject>(reader.Resolve(xyz[1])).Value);
        Assert.Equal(5, Assert.IsType<NumberObject>(reader.Resolve(xyz[2])).DoubleValue, 5);
        Assert.Equal(295, Assert.IsType<NumberObject>(reader.Resolve(xyz[3])).DoubleValue, 5);
        Assert.Equal(2.0, Assert.IsType<NumberObject>(reader.Resolve(xyz[4])).DoubleValue, 5);
    }

    [Fact]
    public void PlainOutlineItem_EmitsNoStyleKeys()
    {
        var document = ThreePages();
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(new OutlineItem("Plain", OutlineTarget.ToPage(0)));

        var reader = DocumentReader.Parse(rendered.ToArray());
        var item = Resolve(reader, Outlines(reader)["First"]);
        Assert.False(item.ContainsKey("C"));
        Assert.False(item.ContainsKey("F"));
    }
}

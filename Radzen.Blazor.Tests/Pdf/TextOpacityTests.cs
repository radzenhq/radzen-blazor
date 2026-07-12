#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Run.Opacity paints a text run through a page /ExtGState (/ca fill, /CA stroke)
// selected with the gs operator; Container.Opacity extends past the box decoration
// to the child content's draws (fills, text, images). All of it is additive:
// default opacity emits no ExtGState and no gs, byte-identically.
public class TextOpacityTests
{
    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static string PageText(DocumentBuilder builder, int index = 0)
    {
        var reader = BuildTestSupport.Read(builder);
        var (page, _) = BuildTestSupport.PageLeaves(reader)[index];
        return Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));
    }

    private static DictionaryObject? ExtGStates(DocumentBuilder builder, int index = 0)
    {
        var reader = BuildTestSupport.Read(builder);
        var (_, resources) = BuildTestSupport.PageLeaves(reader)[index];
        return resources is not null
            && resources.TryGetValue("ExtGState", out var states)
            && reader.Resolve(states!) is DictionaryObject dict
            ? dict
            : null;
    }

    private static double Alpha(DictionaryObject states, string key, string entry)
        => ((NumberObject)((DictionaryObject)states[key]!)[entry]!).DoubleValue;

    [Fact]
    public void RunOpacity_RegistersExtGStateAndWrapsShowInGs()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Solid ");
        paragraph.Inlines.Add("Faded").Opacity = 0.5;
        section.Blocks.Add(paragraph);

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0.5, Alpha(states!, "GS0", "CA"), 6);

        var text = PageText(builder);
        var gs = text.IndexOf("q\n/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        var show = text.IndexOf(" Tj", gs, System.StringComparison.Ordinal);
        Assert.True(show > gs);
        Assert.True(text.IndexOf("Q\n", show, System.StringComparison.Ordinal) > show);
        Assert.Equal(1, BuildTestSupport.CountOccurrences(text, " gs"));
    }

    [Fact]
    public void RunOpacity_DedupsAcrossRunsByValue()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("First").Opacity = 0.5;
        paragraph.Inlines.Add(" Second").Opacity = 0.5;
        section.Blocks.Add(paragraph);

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Single(states!.Keys);
        Assert.Equal(2, BuildTestSupport.CountOccurrences(PageText(builder), "/GS0 gs"));
    }

    [Fact]
    public void ContainerOpacity_AppliesToChildText()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.5 });
        container.Blocks.Add(Text("Inside"));

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Tj", gs, System.StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void ContainerOpacity_AppliesToChildImage()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.5 });
        container.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Do", gs, System.StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void ContainerOpacity_MultipliesWithChildImageOpacity()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.5 });
        container.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg")).Opacity = 0.5;

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Alpha(states!, key, "ca") == 0.25);
    }

    [Fact]
    public void ContainerOpacity_AppliesToNestedTableContent()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.5 });
        var table = container.Blocks.Add(new Table());
        table.Columns.Add(Unit.FromPoint(100));
        var cell = table.Rows.Add().Cells[0];
        cell.Background = Color.FromRgb(200, 200, 200);
        cell.Blocks.Add(Text("Nested"));

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" re f", gs, System.StringComparison.Ordinal) > gs);
        Assert.True(text.IndexOf(" Tj", gs, System.StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void NestedContainerOpacity_Multiplies()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var outer = section.Blocks.Add(new Container { Opacity = 0.5 });
        var inner = outer.Blocks.Add(new Container { Opacity = 0.5 });
        inner.Blocks.Add(Text("Deep"));

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Alpha(states!, key, "ca") == 0.25);
    }

    [Fact]
    public void DefaultOpacity_IsByteIdenticalToUnset()
    {
        static byte[] Content(bool set)
        {
            var builder = new DocumentBuilder();
            var section = builder.Sections.Add();
            var paragraph = new Paragraph();
            var run = paragraph.Inlines.Add("Plain");
            section.Blocks.Add(paragraph);
            var container = section.Blocks.Add(new Container { Background = Color.FromRgb(200, 200, 200) });
            container.Blocks.Add(Text("Boxed"));
            container.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
            if (set)
            {
                run.Opacity = 1;
                container.Opacity = 1;
            }

            var reader = BuildTestSupport.Read(builder);
            var (page, _) = BuildTestSupport.PageLeaves(reader)[0];
            return BuildTestSupport.Content(reader, page);
        }

        Assert.Equal(Content(set: false), Content(set: true));
        Assert.DoesNotContain(" gs\n", Encoding.ASCII.GetString(Content(set: true)), System.StringComparison.Ordinal);
    }
}

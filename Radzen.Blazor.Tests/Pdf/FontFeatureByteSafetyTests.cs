#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

public class FontFeatureByteSafetyTests
{
    private static DocumentBuilder BuildDocument()
    {
        var builder = new DocumentBuilder();
        builder.Fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));

        var section = builder.Sections.Add();

        var justified = new Paragraph { Alignment = HorizontalAlignment.Justify };
        var body = justified.Inlines.Add(
            "A table-heavy layout with several mid-size words and a well-known hyphenated-compound token to wrap.");
        body.Font.Name = "Liberation Sans";
        body.Font.Size = 12;
        section.Blocks.Add(justified);

        var tabbed = new Paragraph();
        tabbed.TabStops.AddTabStop(Unit.FromPoint(200), TabAlignment.Right);
        var tabRun = tabbed.Inlines.Add("Label\tValue");
        tabRun.Font.Name = "Liberation Sans";
        tabRun.Font.Size = 12;
        section.Blocks.Add(tabbed);

        return builder;
    }

    [Fact]
    public void UnfeaturedDocument_BuildsByteIdentically()
    {
        var first = BuildDocument().ToArray();
        var second = BuildDocument().ToArray();

        Assert.Equal(first, second);
    }
}

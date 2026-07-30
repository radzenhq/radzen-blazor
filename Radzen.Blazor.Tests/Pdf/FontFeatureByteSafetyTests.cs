#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FontFeatureByteSafetyTests
{
    private static Document BuildDocument()
    {
        var document = new Document();
        document.Fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));

        var section = document.Sections.Add();

        var justified = new Paragraph { Alignment = HorizontalAlignment.Justify };
        var body = justified.Inlines.Add(
            "A table-heavy layout with several mid-size words and a well-known hyphenated-compound token to wrap.");
        body.Font.Family = "Liberation Sans";
        body.Font.Size = 12;
        section.Blocks.Add(justified);

        var tabbed = new Paragraph();
        tabbed.TabStops.AddTabStop(Unit.FromPoint(200), TabAlignment.Right);
        var tabRun = tabbed.Inlines.Add("Label\tValue");
        tabRun.Font.Family = "Liberation Sans";
        tabRun.Font.Size = 12;
        section.Blocks.Add(tabbed);

        return document;
    }

    [Fact]
    public void UnfeaturedDocument_BuildsByteIdentically()
    {
        var first = new DocumentRenderer().ToArray(BuildDocument());
        var second = new DocumentRenderer().ToArray(BuildDocument());

        Assert.Equal(first, second);
    }
}

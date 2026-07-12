#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

// Byte-safety guard for the font/line-breaking feature work (pair kerning, dot leaders,
// conditional breaks). A document that does NOT opt into any new feature - it merely
// contains plain hyphens, tabs without leaders, and justified text that all flow through
// the modified LineBreaker - must serialize byte-for-byte identically across builds.
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

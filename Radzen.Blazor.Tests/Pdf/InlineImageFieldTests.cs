#nullable enable

using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class InlineImageFieldTests
{
    [Fact]
    public void FooterParagraph_WithInlineImageAndPageNumber_RendersBoth()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(40));
        section.Blocks.AddParagraph("body");

        var footer = section.Footer.Blocks.AddParagraph();
        var image = footer.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(40);
        image.Height = Unit.FromPoint(30);
        var number = new PageNumberField();
        footer.Inlines.Add(number);
        number.Font.Size = 12;

        var reader = BuildTestSupport.Read(document);
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));

        Assert.Matches(new Regex(@"40(?:\.0+)?\s+0\S*\s+0\S*\s+30(?:\.0+)?\s+[-\d.]+\s+[-\d.]+\s+cm"), content);
        Assert.Contains("(1) Tj", content);
    }
}

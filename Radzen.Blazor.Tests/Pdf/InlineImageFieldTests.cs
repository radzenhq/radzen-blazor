#nullable enable

using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

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
        var content = ContentTestHelpers.PageContent(reader, 0);
        var operations = ContentStreamTokenizer.Parse(content);

        Assert.Contains(
            operations,
            operation => operation.Operator == "cm"
                && operation.Num(0) == 40 && operation.Num(1) == 0
                && operation.Num(2) == 0 && operation.Num(3) == 30);
        Assert.Contains(
            operations,
            operation => operation.Operator == "Tj"
                && Encoding.Latin1.GetString(operation.Operands[0].Bytes) == "1");
    }
}

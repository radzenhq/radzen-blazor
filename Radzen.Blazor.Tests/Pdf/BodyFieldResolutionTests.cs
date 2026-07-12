#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Page-number/count fields placed in a SECTION BODY paragraph (not just a header/footer
// band or a band-table cell) must resolve to the real page number/count. They used to
// emit the literal "0" baked into the field, because GeneratePage emitted body lines
// without the field-substitution pass that the band and cell paths run.
public class BodyFieldResolutionTests
{
    private static string[] TextRuns(DocumentReader reader, int page)
    {
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, page));
        var list = new List<string>();
        foreach (Match m in Regex.Matches(content, @"\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            list.Add(m.Groups[1].Value);
        }

        return list.ToArray();
    }

    [Fact]
    public void BodyParagraphWithFields_RendersActualPageNumberAndCount()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margin = Unit.FromPoint(40);

        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("page ").Font.Size = 12;
        paragraph.Inlines.Add(new PageNumberField()).Font.Size = 12;
        paragraph.Inlines.Add(" of ").Font.Size = 12;
        paragraph.Inlines.Add(new PageCountField()).Font.Size = 12;

        var reader = BuildTestSupport.Read(builder);
        var runs = TextRuns(reader, 0);

        Assert.DoesNotContain("0", runs);
        Assert.Equal("page 1 of 1", string.Concat(runs));
    }
}

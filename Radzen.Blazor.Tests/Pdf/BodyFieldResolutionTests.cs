#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class BodyFieldResolutionTests
{
    private static void AddField(Paragraph paragraph, TextInline field)
    {
        field.Font.Size = 12;
        paragraph.Inlines.Add(field);
    }

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
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(500));
        section.Margins.SetAll(Unit.FromPoint(40));

        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("page ").Font.Size = 12;
        AddField(paragraph, new PageNumberField());
        paragraph.Inlines.Add(" of ").Font.Size = 12;
        AddField(paragraph, new PageCountField());

        var reader = BuildTestSupport.Read(document);
        var runs = TextRuns(reader, 0);

        Assert.DoesNotContain("0", runs);
        Assert.Equal("page 1 of 1", string.Concat(runs));
    }
}

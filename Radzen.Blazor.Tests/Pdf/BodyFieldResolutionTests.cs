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

        var paragraph = section.Blocks.Add(new Paragraph());
        paragraph.Inlines.Add("page ").Font.Size = 12;
        AddField(paragraph, new PageNumberField());
        paragraph.Inlines.Add(" of ").Font.Size = 12;
        AddField(paragraph, new PageCountField());

        var reader = BuildTestSupport.Read(document);
        var runs = TextRuns(reader, 0);

        Assert.DoesNotContain("0", runs);
        Assert.Equal("page 1 of 1", string.Concat(runs));
    }

    [Fact]
    public void BodyParagraphWithFields_SplitAcrossPages_ResolvesEachPageRange()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(180));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Blocks.Add(new Paragraph(string.Join("\n", Enumerable.Repeat("filler", 7)))).Font.Size = 12;

        var paragraph = section.Blocks.Add(new Paragraph());
        for (var line = 0; line < 6; line++)
        {
            if (line > 0)
            {
                paragraph.Inlines.Add("\n").Font.Size = 12;
            }

            paragraph.Inlines.Add("field page ").Font.Size = 12;
            AddField(paragraph, new PageNumberField());
            paragraph.Inlines.Add(" of ").Font.Size = 12;
            AddField(paragraph, new PageCountField());
        }

        var reader = BuildTestSupport.Read(document);

        Assert.Equal(2, DocumentLoadTests.PageCount(reader));
        Assert.Contains("field page 1 of 2", string.Concat(TextRuns(reader, 0)), System.StringComparison.Ordinal);
        Assert.Contains("field page 2 of 2", string.Concat(TextRuns(reader, 1)), System.StringComparison.Ordinal);
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldResolutionTests
{
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double Margin = 40;
    private const double FontSize = 12;

    private static (string Text, double X, double Y)[] Runs(DocumentReader reader, int page)
    {
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, page));
        var list = new List<(string, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            list.Add((m.Groups[3].Value,
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return list.ToArray();
    }

    private static (Document Builder, Section Section) Author()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        section.Blocks.AddParagraph("body one");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("body two");
        return (document, section);
    }

    private static void Sized(Inline inline) => ((TextInline)inline).Font.Size = FontSize;

    [Fact]
    public void FooterTableCell_PageNumberField_RendersActualPageNumberPerPage()
    {
        var (document, section) = Author();
        var table = section.Footer.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        var row = table.Rows.Add();
        Sized(row.Cells[0].Blocks.AddParagraph().Inlines.Add("Confidential"));

        var field = row.Cells[1].Blocks.AddParagraph();
        Sized(field.Inlines.Add("Page "));
        Sized(field.Inlines.Add(new PageNumberField()));
        Sized(field.Inlines.Add(" of "));
        Sized(field.Inlines.Add(new PageCountField()));

        var reader = BuildTestSupport.Read(document);

        for (var page = 0; page < 2; page++)
        {
            var runs = Runs(reader, page);
            Assert.DoesNotContain(runs, r => r.Text == "0");

            var footer = runs
                .Where(r => !r.Text.StartsWith("Confidential", StringComparison.Ordinal)
                    && !r.Text.StartsWith("body", StringComparison.Ordinal))
                .OrderBy(r => r.X)
                .ToArray();
            Assert.Equal($"Page {page + 1} of 2", string.Concat(footer.Select(r => r.Text)));
        }
    }

    [Fact]
    public void WrappingBandParagraphWithField_EmitsAllLines()
    {
        var (document, section) = Author();
        var footer = section.Footer.Blocks.AddParagraph();
        Sized(footer.Inlines.Add(
            "This is a deliberately long footer sentence that has to wrap across several lines within the narrow content box, ending on page "));
        Sized(footer.Inlines.Add(new PageNumberField()));

        var reader = BuildTestSupport.Read(document);
        var runs = Runs(reader, 0);

        var bodyY = Assert.Single(runs, r => r.Text.StartsWith("body", StringComparison.Ordinal)).Y;
        var footerRuns = runs.Where(r => r.Y < bodyY - 1).ToArray();

        var baselines = footerRuns.Select(r => Math.Round(r.Y, 1)).Distinct().Count();
        Assert.True(baselines >= 2, $"a wrapping footer field paragraph must emit multiple lines, got {baselines}");
        var joined = string.Concat(footerRuns.OrderByDescending(r => r.Y).Select(r => r.Text));
        Assert.Contains("page 1", joined, StringComparison.Ordinal);
    }
}

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
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class FooterRightTabTests
{
    private const double Tol = 1.0;
    private const double EdgeTol = 1.5;
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double Margin = 40;
    private const double FontSize = 12;

    private static (Document Builder, Section Section) Author()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(Margin));
        return (document, section);
    }

    private const double ContentWidth = PageWidth - (2 * Margin);

    private static void RightTabAtContentEdge(Paragraph paragraph)
        => paragraph.TabStops.Add(Unit.FromPoint(ContentWidth), TabAlignment.Right);

    private static Inline Sized(Inline inline)
    {
        ((TextInline)inline).Font.Size = FontSize;
        return inline;
    }

    private static double Measure(string text)
        => new FontCollection().MeasureText(text, new Font { Size = FontSize });

    private static List<(string Text, double X, double Y)> TextRuns(DocumentReader reader, int pageIndex)
    {
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, pageIndex));
        var runs = new List<(string, double, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value,
                double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static List<(string Text, double X, double Y)> FirstPageRuns(Document document)
        => TextRuns(BuildTestSupport.Read(document), 0);

    [Fact]
    public void RightTabStopAtTheContentEdge_TextAfterTab_IsFlushRight_OnTheSameBaseline()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.Add(new Paragraph());
        Sized(paragraph.Inlines.Add("Left\tRight"));
        RightTabAtContentEdge(paragraph);

        var runs = FirstPageRuns(document);
        var left = Assert.Single(runs, r => r.Text == "Left");
        var right = Assert.Single(runs, r => r.Text == "Right");

        Assert.True(Math.Abs(left.X - Margin) <= Tol,
            $"left text must start at the left content edge {Margin}, started at {left.X:F2}");
        Assert.True(Math.Abs(left.Y - right.Y) <= Tol,
            $"text before and after the right tab must share one baseline, got {left.Y:F2} vs {right.Y:F2}");

        var rightEdge = right.X + Measure("Right");
        Assert.True(Math.Abs(rightEdge - (PageWidth - Margin)) <= EdgeTol,
            $"text after the last tab must end flush at the right content edge {PageWidth - Margin}, ended at {rightEdge:F2}");
    }

    [Fact]
    public void RightTabStopAtTheContentEdge_EarlierTabs_UseTheirOwnLeftStop()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.Add(new Paragraph());
        Sized(paragraph.Inlines.Add("ID:\tAAA\tRight"));
        paragraph.TabStops.Add(Unit.FromPoint(48), TabAlignment.Left);
        RightTabAtContentEdge(paragraph);

        var runs = FirstPageRuns(document);
        var label = Assert.Single(runs, r => r.Text == "ID:");
        var middle = Assert.Single(runs, r => r.Text == "AAA");
        var right = Assert.Single(runs, r => r.Text == "Right");

        Assert.True(Math.Abs(label.Y - middle.Y) <= Tol && Math.Abs(label.Y - right.Y) <= Tol,
            "all three fragments must sit on one baseline");

        var advance = middle.X - label.X;
        Assert.True(advance >= 30 && advance <= 80,
            $"a tab before the last one must land on its own left stop, advanced {advance:F2}pt");

        var rightEdge = right.X + Measure("Right");
        Assert.True(Math.Abs(rightEdge - (PageWidth - Margin)) <= EdgeTol,
            $"only the text after the LAST tab is flush right, ended at {rightEdge:F2} vs edge {PageWidth - Margin}");
    }

    [Fact]
    public void Paragraph_WithoutTabStops_KeepsTheDefaultLeftTabGrid()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.Add(new Paragraph());
        Sized(paragraph.Inlines.Add("Left\tRight"));

        var runs = FirstPageRuns(document);
        var left = Assert.Single(runs, r => r.Text == "Left");
        var right = Assert.Single(runs, r => r.Text == "Right");

        var advance = right.X - left.X;
        Assert.True(advance >= 30 && advance <= 80,
            $"without explicit stops a tab must keep the default left tab grid, advanced {advance:F2}pt");
    }

    private static Document TwoPagesWithRightTabFooter()
    {
        var (document, section) = Author();
        var footer = section.Footer.Blocks.Add(new Paragraph());
        Sized(footer.Inlines.Add("Confidential\t"));
        Sized(footer.Inlines.Add("Page "));
        Sized(footer.Inlines.Add(new PageNumberField()));
        Sized(footer.Inlines.Add(" of "));
        Sized(footer.Inlines.Add(new PageCountField()));
        RightTabAtContentEdge(footer);

        section.Blocks.Add(new Paragraph("body one"));
        section.Blocks.Add(new PageBreak());
        section.Blocks.Add(new Paragraph("body two"));
        return document;
    }

    private static List<(string Text, double X, double Y)> FooterRuns(DocumentReader reader, int pageIndex)
    {
        var runs = TextRuns(reader, pageIndex);
        Assert.NotEmpty(runs);
        var footerY = runs.Min(r => r.Y);
        return runs.Where(r => Math.Abs(r.Y - footerY) < Tol).OrderBy(r => r.X).ToList();
    }

    [Fact]
    public void Footer_LeftTextAndPageNumbers_ShareOneBaseline_PerPage()
    {
        var reader = BuildTestSupport.Read(TwoPagesWithRightTabFooter());

        for (var page = 0; page < 2; page++)
        {
            var all = TextRuns(reader, page);
            var confidential = Assert.Single(all, r => r.Text.StartsWith("Confidential", StringComparison.Ordinal));

            var expected = $"Page {page + 1} of 2";
            var numberRuns = all
                .Where(r => !r.Text.StartsWith("Confidential", StringComparison.Ordinal)
                    && !r.Text.StartsWith("body", StringComparison.Ordinal))
                .OrderBy(r => r.X)
                .ToList();
            Assert.NotEmpty(numberRuns);
            Assert.Equal(expected, string.Concat(numberRuns.Select(r => r.Text)));

            foreach (var run in numberRuns)
            {
                Assert.True(Math.Abs(run.Y - confidential.Y) <= Tol,
                    $"page {page + 1}: '{run.Text}' baseline {run.Y:F2} must equal the left footer baseline {confidential.Y:F2}");
            }
        }
    }

    [Fact]
    public void Footer_PageNumbers_AreFlushAgainstTheRightContentEdge()
    {
        var reader = BuildTestSupport.Read(TwoPagesWithRightTabFooter());

        for (var page = 0; page < 2; page++)
        {
            var footer = FooterRuns(reader, page);
            var expected = $"Page {page + 1} of 2";
            var numberRuns = footer
                .Where(r => !r.Text.StartsWith("Confidential", StringComparison.Ordinal))
                .ToList();
            Assert.NotEmpty(numberRuns);
            Assert.Equal(expected, string.Concat(numberRuns.Select(r => r.Text)));

            var rightEdge = numberRuns[0].X + Measure(expected);
            Assert.True(Math.Abs(rightEdge - (PageWidth - Margin)) <= EdgeTol,
                $"page {page + 1}: '{expected}' must end flush at the right content edge {PageWidth - Margin}, ended at {rightEdge:F2}");
            Assert.True(numberRuns[0].X > PageWidth / 2,
                $"page {page + 1}: the page-number text must sit in the right half of the page, started at {numberRuns[0].X:F2}");
        }
    }

    [Fact]
    public void Footer_Band_SitsASensibleDistanceAboveThePageBottom()
    {
        var reader = BuildTestSupport.Read(TwoPagesWithRightTabFooter());

        for (var page = 0; page < 2; page++)
        {
            var footer = FooterRuns(reader, page);
            var confidential = Assert.Single(footer, r => r.Text.StartsWith("Confidential", StringComparison.Ordinal));

            Assert.True(confidential.Y >= 20,
                $"page {page + 1}: footer baseline {confidential.Y:F2} sits too close to the physical page bottom");
            Assert.True(confidential.Y <= 100,
                $"page {page + 1}: footer baseline {confidential.Y:F2} floats too far above the page bottom");
        }
    }
}

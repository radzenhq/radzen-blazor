#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Blazor.Tests.Isolated;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

public class HeaderFooterDistanceTests
{
    private const double Tol = 0.5;
    private const double PageWidth = 400;
    private const double PageHeight = 500;
    private const double FontSize = 12;

    private static (double Height, double Baseline) LineMetrics(double size)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Xg");
        run.Font.Size = size;
        var box = IsolatedLineBreaker.Break(paragraph, 100000, new FontCollection())[0];
        return (box.Height, box.Baseline);
    }

    private static Paragraph Text(string text, double size = FontSize)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Size = size;
        return paragraph;
    }

    private static (Document Builder, Section Section) Author(double margin)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(PageWidth), Unit.FromPoint(PageHeight));
        section.Margins.SetAll(Unit.FromPoint(margin));
        return (document, section);
    }

    private static List<(string Text, double Y)> TextRuns(string content)
    {
        var runs = new List<(string, double)>();
        foreach (Match m in Regex.Matches(content, @"(-?[\d.]+)\s+(-?[\d.]+)\s+Td\s*\((.*?)\)\s*Tj", RegexOptions.Singleline))
        {
            runs.Add((m.Groups[3].Value, double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        return runs;
    }

    private static double Distance(Section section, string name)
    {
        var property = typeof(Section).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, $"Section must expose a public {name} property");
        Assert.Equal(typeof(Unit), property!.PropertyType);
        return ((Unit)property.GetValue(section)!).Point;
    }

    private static void SetDistance(Section section, string name, double points)
    {
        var property = typeof(Section).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, $"Section must expose a public {name} property");
        property!.SetValue(section, Unit.FromPoint(points));
    }

    [Fact]
    public void Header_IsNotFlushToPhysicalPageTop()
    {
        var (document, section) = Author(margin: 20);
        section.Header.Blocks.Add(Text("HDR"));
        section.Blocks.Add(Text("BODY"));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var headerY = Assert.Single(runs, r => r.Text == "HDR").Y;
        var (_, baseline) = LineMetrics(FontSize);

        var headerTopEdge = headerY + baseline;
        Assert.True(
            PageHeight - headerTopEdge >= 8,
            $"the header line top edge {headerTopEdge:F2} must sit clearly below the page top {PageHeight:F2}");
    }

    [Fact]
    public void Footer_SitsNearPageBottom_NotHangingFromBody()
    {
        var (document, section) = Author(margin: 20);
        section.Footer.Blocks.Add(Text("FTR"));
        section.Blocks.Add(Text("BODY"));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var footerY = Assert.Single(runs, r => r.Text == "FTR").Y;
        var (lineHeight, baseline) = LineMetrics(FontSize);

        var footerBottomEdge = footerY - (lineHeight - baseline);
        Assert.True(
            footerBottomEdge >= 15,
            $"the footer band bottom edge {footerBottomEdge:F2} must end well above the page bottom");
    }

    [Fact]
    public void DefaultHeaderDistance_PositionsHeaderAndReducesBody()
    {
        var (document, section) = Author(margin: 40);
        var distance = Distance(section, "HeaderDistance");
        Assert.InRange(distance, 30.0, 40.0);

        section.Header.Blocks.Add(Text("HDR"));
        section.Blocks.Add(Text("BODY"));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var headerY = Assert.Single(runs, r => r.Text == "HDR").Y;
        var bodyY = Assert.Single(runs, r => r.Text == "BODY").Y;
        var (lineHeight, baseline) = LineMetrics(FontSize);

        Assert.True(
            Math.Abs(headerY - (PageHeight - distance - baseline)) <= Tol,
            $"header baseline {headerY:F2} must start HeaderDistance {distance:F2} below the page top");

        var bandBottomEdge = PageHeight - distance - lineHeight;
        var bodyTopEdge = bodyY + baseline;
        Assert.True(
            bodyTopEdge <= bandBottomEdge + Tol,
            $"body top edge {bodyTopEdge:F2} must be at or below the header band bottom {bandBottomEdge:F2}");
    }

    [Fact]
    public void DefaultFooterDistance_PositionsFooterAndReducesBody()
    {
        var (document, section) = Author(margin: 40);
        var distance = Distance(section, "FooterDistance");
        Assert.InRange(distance, 30.0, 40.0);

        section.Footer.Blocks.Add(Text("FTR"));

        var (lineHeight, baseline) = LineMetrics(FontSize);
        var fullBodyLines = (int)((PageHeight - 2 * 40) / lineHeight);
        for (var i = 0; i < fullBodyLines; i++)
        {
            section.Blocks.Add(Text($"B{i}"));
        }

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var footerY = Assert.Single(runs, r => r.Text == "FTR").Y;
        var bodyYs = runs.Where(r => r.Text.StartsWith("B", StringComparison.Ordinal)).Select(r => r.Y).ToList();
        Assert.NotEmpty(bodyYs);

        var bandTopEdge = distance + lineHeight;
        Assert.True(
            Math.Abs(footerY - (bandTopEdge - baseline)) <= Tol,
            $"footer baseline {footerY:F2} must end FooterDistance {distance:F2} above the page bottom");

        Assert.All(bodyYs, y => Assert.True(
            y - (lineHeight - baseline) >= bandTopEdge - Tol,
            $"body line bottom {y - (lineHeight - baseline):F2} must stay above the footer band top {bandTopEdge:F2}"));
    }

    [Fact]
    public void ExplicitDistances_ControlBandPositions_WithoutMovingBodyInsideMargins()
    {
        const double margin = 60;
        const double headerDistance = 20;
        const double footerDistance = 25;

        var (document, section) = Author(margin);
        SetDistance(section, "HeaderDistance", headerDistance);
        SetDistance(section, "FooterDistance", footerDistance);

        section.Header.Blocks.Add(Text("HDR"));
        section.Footer.Blocks.Add(Text("FTR"));
        section.Blocks.Add(Text("BODY"));

        var runs = TextRuns(CascadeTestSupport.FirstPageContent(document));
        var headerY = Assert.Single(runs, r => r.Text == "HDR").Y;
        var footerY = Assert.Single(runs, r => r.Text == "FTR").Y;
        var bodyY = Assert.Single(runs, r => r.Text == "BODY").Y;
        var (lineHeight, baseline) = LineMetrics(FontSize);

        Assert.True(
            Math.Abs(headerY - (PageHeight - headerDistance - baseline)) <= Tol,
            $"header baseline {headerY:F2} must reflect HeaderDistance {headerDistance}");
        Assert.True(
            Math.Abs(footerY - (footerDistance + lineHeight - baseline)) <= Tol,
            $"footer baseline {footerY:F2} must reflect FooterDistance {footerDistance}");

        Assert.True(
            Math.Abs(bodyY - (PageHeight - margin - baseline)) <= Tol,
            $"body baseline {bodyY:F2} must stay at the margin when the bands fit inside it");
    }
}

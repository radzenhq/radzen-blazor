#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class HyperlinkPageNumberRegressionTests
{
    private const double Tol = 1.0;
    private const string Url = "https://www.radzen.com/blazor-studio/";

    private static (Document Builder, Section Section) Author(double width = 400, double height = 300)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(40));
        return (document, section);
    }

    private static void SetLink(Inline inline, string url)
    {
        var property = typeof(Inline).GetProperty("Link", BindingFlags.Public | BindingFlags.Instance);
        Assert.True(property is not null, "Inline.Link public property is missing");
        Assert.True(property!.PropertyType == typeof(string), $"Inline.Link must be a string, was {property.PropertyType}");
        property.SetValue(inline, url);
    }

    private static TextInline Field(string typeName)
    {
        var type = typeof(Inline).Assembly.GetType($"Radzen.Documents.{typeName}");
        Assert.True(type is not null, $"public type Radzen.Documents.{typeName} is missing");
        Assert.True(
            typeof(TextInline).IsAssignableFrom(type),
            $"{typeName} must derive from TextInline so it can be added to Paragraph.Inlines and carry a font");
        var instance = Activator.CreateInstance(type!);
        return Assert.IsAssignableFrom<TextInline>(instance);
    }

    private static List<(double X1, double Y1, double X2, double Y2, string Uri)> LinkAnnotations(DocumentReader reader, int pageIndex)
    {
        var page = ContentTestHelpers.Kid(reader, pageIndex);
        var links = new List<(double, double, double, double, string)>();
        if (!page.TryGetValue("Annots", out var annotsObject) || reader.Resolve(annotsObject!) is not ArrayObject annots)
        {
            return links;
        }

        for (var i = 0; i < annots.Count; i++)
        {
            if (reader.Resolve(annots[i]) is not DictionaryObject annot
                || !annot.TryGetValue("Subtype", out var subtype)
                || reader.Resolve(subtype!) is not NameObject subtypeName
                || subtypeName.Value != "Link")
            {
                continue;
            }

            Assert.True(annot.TryGetValue("A", out var actionObject), "/Link annotation must carry an /A action");
            var action = Assert.IsType<DictionaryObject>(reader.Resolve(actionObject!));
            var s = Assert.IsType<NameObject>(reader.Resolve(action["S"]));
            Assert.Equal("URI", s.Value);
            var uri = Assert.IsType<StringObject>(reader.Resolve(action["URI"]));

            Assert.True(annot.TryGetValue("Rect", out var rectObject), "/Link annotation must carry a /Rect");
            var rect = Assert.IsType<ArrayObject>(reader.Resolve(rectObject!));
            Assert.Equal(4, rect.Count);
            var n = new double[4];
            for (var j = 0; j < 4; j++)
            {
                n[j] = Assert.IsType<NumberObject>(reader.Resolve(rect[j])).DoubleValue;
            }

            links.Add((Math.Min(n[0], n[2]), Math.Min(n[1], n[3]), Math.Max(n[0], n[2]), Math.Max(n[1], n[3]), uri.Value));
        }

        return links;
    }

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

    [Fact]
    public void LinkedRun_EmitsLinkAnnotation_WithUriOverTheTextRect()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("Visit ");
        SetLink(paragraph.Inlines.Add("Radzen"), Url);

        var reader = BuildTestSupport.Read(document);
        var link = Assert.Single(LinkAnnotations(reader, 0));
        Assert.Equal(Url, link.Uri);

        var runs = TextRuns(reader, 0);
        var visit = Assert.Single(runs, r => r.Text.Contains("Visit", StringComparison.Ordinal));
        var target = Assert.Single(runs, r => r.Text.EndsWith("Radzen", StringComparison.Ordinal));

        Assert.True(link.X2 > link.X1, "link rect must have positive width");
        Assert.True(link.Y2 > link.Y1, "link rect must have positive height");
        Assert.True(link.X1 >= visit.X - Tol, $"link rect x1 {link.X1:F2} must not start before the line at {visit.X:F2}");
        if (target.Text == "Radzen")
        {
            Assert.True(link.X1 > visit.X + Tol, $"link rect x1 {link.X1:F2} must not cover the unlinked prefix at {visit.X:F2}");
            Assert.True(link.X1 <= target.X + Tol && target.X <= link.X2, $"link rect [{link.X1:F2},{link.X2:F2}] must cover the linked text x {target.X:F2}");
        }

        Assert.True(link.Y1 - Tol <= target.Y && target.Y <= link.Y2 + Tol, $"link rect [{link.Y1:F2},{link.Y2:F2}] must cover the linked text baseline {target.Y:F2}");
    }

    // ISO 19005-3 6.3.2: every annotation must set the Print flag (bit 3 = 4)
    // and clear Hidden/NoView.
    [Fact]
    public void LinkAnnotation_SetsPrintFlag_ForPdfAConformance()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.AddParagraph();
        SetLink(paragraph.Inlines.Add("Radzen"), Url);

        var reader = BuildTestSupport.Read(document);
        var page = ContentTestHelpers.Kid(reader, 0);
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        var annot = Assert.IsType<DictionaryObject>(reader.Resolve(annots[0]));

        Assert.True(annot.ContainsKey("F"), "Link annotation must carry the /F flags key (PDF/A 6.3.2)");
        var flags = Assert.IsType<NumberObject>(reader.Resolve(annot["F"])).IntValue;
        Assert.True((flags & 4) != 0, "Print flag (bit 3) must be set");
        Assert.True((flags & 2) == 0, "Hidden flag (bit 2) must be clear");
        Assert.True((flags & 32) == 0, "NoView flag (bit 6) must be clear");
    }

    [Fact]
    public void LinkOnSecondPage_AnnotationLandsOnThatPage()
    {
        var (document, section) = Author();
        section.Blocks.AddParagraph("page one body");
        section.Blocks.AddPageBreak();
        var paragraph = section.Blocks.AddParagraph();
        SetLink(paragraph.Inlines.Add("second page link"), Url);

        var reader = BuildTestSupport.Read(document);
        Assert.Empty(LinkAnnotations(reader, 0));
        var link = Assert.Single(LinkAnnotations(reader, 1));
        Assert.Equal(Url, link.Uri);
    }

    [Fact]
    public void WrappedLink_EmitsOneAnnotationPerLineFragment()
    {
        var (document, section) = Author(width: 200);
        var paragraph = section.Blocks.AddParagraph();
        SetLink(paragraph.Inlines.Add(
            "an intentionally long hyperlink label that cannot possibly fit on a single narrow line"), Url);

        var reader = BuildTestSupport.Read(document);
        var links = LinkAnnotations(reader, 0);

        Assert.True(links.Count >= 2, $"a wrapped link must emit one annotation per fragment, got {links.Count}");
        Assert.All(links, l => Assert.Equal(Url, l.Uri));
        Assert.Equal(links.Count, links.Select(l => Math.Round(l.Y1, 1)).Distinct().Count());
    }

    private static string FooterLine(DocumentReader reader, int pageIndex)
    {
        var runs = TextRuns(reader, pageIndex);
        Assert.NotEmpty(runs);
        var footerY = runs.Min(r => r.Y);
        return string.Concat(runs
            .Where(r => Math.Abs(r.Y - footerY) < Tol)
            .OrderBy(r => r.X)
            .Select(r => r.Text));
    }

    private static Document ThreePageDocumentWithNumberedFooter()
    {
        var (document, section) = Author();
        var footer = section.Footer.Blocks.AddParagraph();
        footer.Inlines.Add("Page ");
        footer.Inlines.Add(Field("PageNumberField"));
        footer.Inlines.Add(" of ");
        footer.Inlines.Add(Field("PageCountField"));

        section.Blocks.AddParagraph("body one");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("body two");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("body three");
        return document;
    }

    [Fact]
    public void Footer_PageNumberAndCountFields_ResolvePerPage()
    {
        var reader = BuildTestSupport.Read(ThreePageDocumentWithNumberedFooter());

        var kids = Assert.IsType<ArrayObject>(reader.Resolve(ContentTestHelpers.PagesNode(reader)["Kids"]));
        Assert.Equal(3, kids.Count);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal($"Page {i + 1} of 3", FooterLine(reader, i));
        }
    }

    [Fact]
    public void Footer_PageNumberFields_SurviveExtractTextRoundTrip()
    {
        var document = BuildTestSupport.Reload(ThreePageDocumentWithNumberedFooter());
        var text = document.ExtractText();

        for (var i = 1; i <= 3; i++)
        {
            Assert.Contains($"Page {i} of 3", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SinglePageDocument_FooterShowsPageOneOfOne()
    {
        var (document, section) = Author();
        var footer = section.Footer.Blocks.AddParagraph();
        footer.Inlines.Add("Page ");
        footer.Inlines.Add(Field("PageNumberField"));
        footer.Inlines.Add(" of ");
        footer.Inlines.Add(Field("PageCountField"));
        section.Blocks.AddParagraph("only page");

        var reader = BuildTestSupport.Read(document);
        Assert.Equal("Page 1 of 1", FooterLine(reader, 0));
    }
}

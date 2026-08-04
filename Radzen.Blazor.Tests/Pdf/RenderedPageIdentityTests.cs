#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class RenderedPageIdentityTests
{
    private static Document ThreePages(bool anchors)
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Page identity";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        for (var page = 0; page < 3; page++)
        {
            var paragraph = BuildTestSupport.AddText(section, $"Page {page} body", BuildTestSupport.Latin);
            if (anchors)
            {
                paragraph.Inlines[0].Anchor = $"anchor{page}";
            }

            if (page < 2)
            {
                section.Blocks.Add(new PageBreak());
            }
        }

        return document;
    }

    private static PortableDocument Tagged(bool anchors)
        => new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(ThreePages(anchors));

    private static string[] PageNumbers(string emission)
        => References("page tree", "Kids", 3, Line(emission, "/Type /Pages"));

    private static List<int> StructurePageOrder(string emission)
    {
        var pages = PageNumbers(emission);
        var order = new List<int>();

        foreach (Match match in Regex.Matches(emission, @"/Pg (\d+) 0 R"))
        {
            var index = Array.IndexOf(pages, match.Groups[1].Value);
            Assert.True(
                index >= 0,
                $"Structure element points at /Pg {match.Groups[1].Value} 0 R, which is not a kid of the page tree.");

            if (order.Count == 0 || order[^1] != index)
            {
                order.Add(index);
            }
        }

        Assert.True(order.Count > 0, $"No structure element carries /Pg.\n{Excerpt(emission)}");
        return order;
    }

    private static int DestinationPage(string emission, string name)
    {
        var destination = Shaped(
            $"named destination '{name}'",
            $@"\({Regex.Escape(name)}\) \[(\d+) 0 R",
            Line(emission, "/Names ["));

        var index = Array.IndexOf(PageNumbers(emission), destination.Groups[1].Value);
        Assert.True(
            index >= 0,
            $"Named destination '{name}' points at {destination.Groups[1].Value} 0 R, which is not a kid of the page tree.");
        return index;
    }

    [Fact]
    public void RemovingATaggedPage_ThrowsNamingTheStructureTree()
    {
        var rendered = Tagged(anchors: false);
        rendered.Pages.RemoveAt(1);

        var error = Assert.Throws<InvalidOperationException>(() => rendered.ToArray());

        Assert.Contains("structure tree", error.Message, StringComparison.Ordinal);
        Assert.Contains("no longer in Pages", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingAnAnchoredPage_ThrowsNamingTheDestination()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: true));
        var plan = rendered.Output!;
        rendered.Output = new Radzen.Documents.Pdf.Output.DocumentOutput(
            plan.Pages, null, plan.Anchors, plan.UnmappedRoles);
        rendered.Pages.RemoveAt(1);

        var error = Assert.Throws<InvalidOperationException>(() => rendered.ToArray());

        Assert.Contains("named destination 'anchor1'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MovingATaggedPage_MovesItsStructureMarks()
    {
        var rendered = Tagged(anchors: false);
        Assert.Equal([0, 1, 2], StructurePageOrder(Emit(rendered)));

        rendered.Pages.Move(0, 2);

        Assert.Equal([2, 0, 1], StructurePageOrder(Emit(rendered)));
    }

    [Fact]
    public void MovingAnAnchoredPage_MovesItsDestination()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: true));
        Assert.Equal(0, DestinationPage(Emit(rendered), "anchor0"));

        rendered.Pages.Move(0, 2);

        Assert.Equal(2, DestinationPage(Emit(rendered), "anchor0"));
        Assert.Equal(0, DestinationPage(Emit(rendered), "anchor1"));
    }

    [Fact]
    public void ReorderingAnUntaggedRenderedDocument_WritesThePagesInTheNewOrder()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: false));
        _ = rendered.ToArray();

        rendered.Pages.Move(2, 0);
        var bytes = rendered.ToArray();

        var emission = Emit(rendered);
        Assert.Equal(3, PageNumbers(emission).Length);
        Carries("page tree", "/Count 3", Line(emission, "/Type /Pages"));
        Assert.Contains("Page 2 body", rendered.Pages[0].ExtractText(), StringComparison.Ordinal);
        Assert.StartsWith("Page 2 body",
            PortableDocument.LoadFromStream(new System.IO.MemoryStream(bytes)).Pages[0].ExtractText(),
            StringComparison.Ordinal);
    }
}

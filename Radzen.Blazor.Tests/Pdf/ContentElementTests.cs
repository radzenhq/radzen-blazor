#nullable enable

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentElementTests
{
    private static string PageContent(PortableDocument document)
    {
        var emission = Emit(document);
        var contents = Shaped("page", @"/Contents (\d+ 0 R|\[[^\]]*\])", Line(emission, "/Type /Page "));
        var streams = new StringBuilder();

        foreach (Match reference in Regex.Matches(contents.Groups[1].Value, @"(\d+) 0 R"))
        {
            streams.Append(IndirectObject(emission, reference.Groups[1].Value)).Append('\n');
        }

        return streams.ToString();
    }

    [Fact]
    public void NonEmptyContent_EmitsContentStream()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 72, 700));

        var content = PageContent(document);

        Carries("page content", "BT\n", content);
        Carries("page content", "ET\n", content);
    }

    [Fact]
    public void ZOrder_TextBeforePath_MatchesAddOrder()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Z", 10, 10));
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(50, 50);

        Shaped("page content", @" Tj\n[\s\S]* m\n", PageContent(document));
    }

    [Fact]
    public void ZOrder_PathBeforeText_MatchesAddOrder()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(50, 50);
        page.Content.Add(new TextContent("Z", 10, 10));

        Shaped("page content", @" m\n[\s\S]* Tj\n", PageContent(document));
    }

    [Fact]
    public void Transform_NonIdentity_EmittedAsCm()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var matrix = Matrix.Scale(2, 3) * Matrix.Translate(10, 20);
        page.Content.Add(new TextContent("T", 0, 0) { Transform = matrix });

        var cm = Shaped(
            "page content",
            @"([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) ([-\d.]+) cm\n",
            PageContent(document));

        double[] expected = [matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F];
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(
                expected[i],
                double.Parse(cm.Groups[i + 1].Value, CultureInfo.InvariantCulture),
                3);
        }
    }

    [Fact]
    public void Transform_Identity_EmitsNoCm()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("T", 0, 0));

        Lacks("page content", " cm\n", PageContent(document));
    }

    [Fact]
    public void IsArtifact_WrapsElementInArtifactMarkedContent()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A", 0, 0) { IsArtifact = true });

        Shaped("page content", @"/Artifact BMC\n[\s\S]* Tj\n[\s\S]*EMC\n", PageContent(document));
    }

    [Fact]
    public void DefaultElement_NotWrappedInArtifact()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A", 0, 0));

        Lacks("page content", "BDC", PageContent(document));
    }

    [Fact]
    public void MultipleElements_OperatorOrderMatchesElementOrder()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("first", 0, 0));
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(1, 1);
        page.Content.Add(new TextContent("third", 0, 0));

        Shaped("page content", @"BT\n[\s\S]* m\n[\s\S]*ET\n", PageContent(document));
    }

    [Fact]
    public void NonEmptyContent_OverridesRawSetContent()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("q Q"));
        page.Content.Add(new TextContent("MARKER", 10, 10));

        Carries("page content", "MARKER", PageContent(document));
    }

    [Fact]
    public void EmptyContent_FallsBackToRawSetContent()
    {
        var raw = Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm");
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(raw);

        Carries("page content", " cm\n", PageContent(document));
    }

    [Fact]
    public void ImageContent_UndecodablePayload_ThrowsOnSave()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent([0, 0, 0, 0]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) });

        Assert.Throws<NotSupportedException>(() => document.ToArray());
    }

}

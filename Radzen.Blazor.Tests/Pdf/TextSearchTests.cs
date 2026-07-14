#nullable enable
using System;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TextSearchTests
{
    [Fact]
    public void FindText_ReturnsPageIndexAndPlausibleBounds()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 20 Tf 72 700 Td (Known word) Tj ET"));

        var hit = Assert.Single(document.FindText("word"));

        Assert.Equal("word", hit.Text);
        Assert.Equal(0, hit.PageIndex);
        Assert.True(hit.Bounds.Left >= 72);
        Assert.True(hit.Bounds.Bottom >= 700);
        Assert.True(hit.Bounds.Width > 0);
        Assert.True(hit.Bounds.Height > 0);
        Assert.Single(hit.Quadrilaterals);
    }

    [Fact]
    public void FindText_RespectsCaseAndWholeWordOptions()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 12 Tf 72 700 Td (Cat catalog CAT) Tj ET"));

        Assert.Equal(3, page.FindText("cat").Count);
        Assert.Equal(2, page.FindText("cat", new TextSearchOptions { WholeWord = true }).Count);
        Assert.Single(page.FindText("CAT", new TextSearchOptions { CaseSensitive = true, WholeWord = true }));
    }

    [Fact]
    public void FindText_MatchAcrossShowOperators_ReturnsTwoQuadsAndSources()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 16 Tf 40 500 Td (Hel) Tj (lo) Tj ET"));

        var hit = Assert.Single(page.FindText("Hello"));

        Assert.Equal(2, hit.Quadrilaterals.Count);
        Assert.Collection(hit.Sources,
            source =>
            {
                Assert.Equal(0, source.OperatorIndex);
                Assert.Equal(0, source.CharacterOffset);
                Assert.Equal(3, source.CharacterLength);
            },
            source =>
            {
                Assert.Equal(1, source.OperatorIndex);
                Assert.Equal(0, source.CharacterOffset);
                Assert.Equal(2, source.CharacterLength);
            });
        Assert.True(hit.Bounds.Width > hit.Quadrilaterals[0].Bounds.Width);
    }

    [Fact]
    public void ExtractPositionedText_AccountsForRotationAndScaling()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("q 0 2 -3 0 300 100 cm BT /F1 10 Tf 10 20 Td (Turn) Tj ET Q"));

        var run = Assert.Single(page.ExtractPositionedText());

        Assert.Equal("Turn", run.Text);
        Assert.Equal(0, run.OperatorIndex);
        Assert.Equal(30, run.Quadrilateral.Bounds.Width, 3);
        Assert.Equal(40, run.Quadrilateral.Bounds.Height, 3);
    }

    [Fact]
    public void PositionedExtraction_DoesNotChangeLegacyExtractTextOutput()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 12 Tf 72 700 Td (Alpha) Tj 0 -20 Td (Beta) Tj ET"));
        var before = page.ExtractText();

        _ = page.ExtractPositionedText();
        _ = page.FindText("Alpha");

        Assert.Equal("Alpha\nBeta", before);
        Assert.Equal(before, page.ExtractText());
    }

    [Fact]
    public void DocumentFindText_ReportsEachMatchingPageIndex()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Content("BT /F1 12 Tf 20 700 Td (Needle) Tj ET"));
        document.Pages.Add().SetContent(Content("BT /F1 12 Tf 20 700 Td (Needle) Tj ET"));

        var hits = document.FindText("Needle");

        Assert.Equal(new[] { 0, 1 }, hits.Select(hit => hit.PageIndex));
    }

    [Fact]
    public void FindText_NormalizedWhitespace_MatchesAcrossLines()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 12 Tf 72 700 Td (Alpha) Tj 0 -20 Td (Beta) Tj ET"));

        var hit = Assert.Single(page.FindText("Alpha Beta", new TextSearchOptions { NormalizeWhitespace = true }));

        Assert.Equal("Alpha\nBeta", hit.Text);
        Assert.Equal(2, hit.Quadrilaterals.Count);
        Assert.Equal(2, hit.Sources.Count);
    }

    [Fact]
    public void FindText_PartialMatchInShowArray_UsesLocalizedKerningAdjustment()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 10 Tf 0 100 Td [(AB) -2000 (CD)] TJ ET"));

        var hit = Assert.Single(page.FindText("CD"));

        Assert.Equal(30, hit.Bounds.Left, 3);
        Assert.Equal(40, hit.Bounds.Right, 3);
        Assert.Equal(10, hit.Bounds.Width, 3);
    }

    [Fact]
    public void FindText_PartialMatchInWordSpacedRun_UsesSpaceAdvance()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Content("BT /F1 10 Tf 10 Tw 0 100 Td (A B) Tj ET"));

        var hit = Assert.Single(page.FindText("B"));

        Assert.Equal(20, hit.Bounds.Left, 3);
        Assert.Equal(25, hit.Bounds.Right, 3);
        Assert.Equal(5, hit.Bounds.Width, 3);
    }

    [Fact]
    public void FindText_RejectsEmptySearchText()
    {
        var document = new Document();
        var page = document.Pages.Add();

        Assert.Throws<ArgumentException>(() => page.FindText(string.Empty));
    }

    private static byte[] Content(string value) => Encoding.ASCII.GetBytes(value);
}

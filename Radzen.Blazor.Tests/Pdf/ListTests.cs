#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class ListTests
{
    private static string PageText(Document document)
        => BuildTestSupport.Reload(document).Pages[0].ExtractText();

    private static List<(double X, string Text)> TextDraws(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        var leaves = BuildTestSupport.PageLeaves(reader);
        var content = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));

        var result = new List<(double, string)>();
        foreach (Match match in Regex.Matches(
            content,
            @"([-\d.]+)\s+([-\d.]+)\s+Td\s*\(((?:\\.|[^)\\])*)\)\s*Tj"))
        {
            result.Add((
                double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                match.Groups[3].Value));
        }

        return result;
    }

    [Fact]
    public void BulletedList_RendersMarkerBeforeEachItem_WithHangingIndent()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Bullet });
        list.HangingIndent = Unit.FromPoint(20);
        list.Items.Add("Alpha");
        list.Items.Add("Beta");

        var draws = TextDraws(document);

        var markers = draws.FindAll(d => Math.Abs(d.X) < 1e-3 && d.Text.Length > 0);
        Assert.Equal(2, markers.Count);
        Assert.All(markers, m => Assert.NotEqual("Alpha", m.Text));

        var alpha = draws.Find(d => d.Text == "Alpha");
        var beta = draws.Find(d => d.Text == "Beta");
        Assert.Equal(20, alpha.X, 3);
        Assert.Equal(20, beta.X, 3);
    }

    [Fact]
    public void OrderedList_NumbersItemsInSequence()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Number });
        list.HangingIndent = Unit.FromPoint(24);
        list.Items.Add("Alpha");
        list.Items.Add("Beta");
        list.Items.Add("Gamma");

        var draws = TextDraws(document);

        var markers = draws.FindAll(d => d.Text is "1." or "2." or "3.");
        Assert.Equal(new[] { "1.", "2.", "3." }, markers.ConvertAll(m => m.Text));
        Assert.All(markers, m => Assert.Equal(0, m.X, 3));

        var text = PageText(document);
        Assert.True(text.IndexOf("Alpha", StringComparison.Ordinal) >= 0);
        Assert.True(
            text.IndexOf("Alpha", StringComparison.Ordinal) < text.IndexOf("Beta", StringComparison.Ordinal),
            "items retain their order");
    }

    [Fact]
    public void OrderedList_WrappedItemContent_AlignsAtHangingIndent()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(120), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));

        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Number });
        list.HangingIndent = Unit.FromPoint(20);
        list.Items.Add("Wrapping content that spills onto a second line");

        var draws = TextDraws(document);

        var marker = draws.Find(d => d.Text == "1.");
        Assert.Equal(0, marker.X, 3);

        var contentLines = draws.FindAll(d => d.Text != "1." && d.Text.Length > 0);
        Assert.True(contentLines.Count >= 2, "content wrapped to at least two lines");
        Assert.All(contentLines, line => Assert.Equal(20, line.X, 3));
    }
}

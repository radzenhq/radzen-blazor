#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ListTests
{
    private static string PageText(DocumentBuilder builder)
        => BuildTestSupport.Reload(builder).Pages[0].ExtractText();

    // Every `x y Td (literal) Tj` text placement on the first page, in emission order.
    private static List<(double X, string Text)> TextDraws(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
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
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margin = Unit.FromPoint(0);

        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.HangingIndent = Unit.FromPoint(20);
        list.AddItem("Alpha");
        list.AddItem("Beta");

        var draws = TextDraws(builder);

        // A non-empty bullet marker at the left edge and item text at the hanging indent, per item.
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
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margin = Unit.FromPoint(0);

        var list = section.Blocks.AddList(ListStyle.Number);
        list.HangingIndent = Unit.FromPoint(24);
        list.AddItem("Alpha");
        list.AddItem("Beta");
        list.AddItem("Gamma");

        var draws = TextDraws(builder);

        var markers = draws.FindAll(d => d.Text is "1." or "2." or "3.");
        Assert.Equal(new[] { "1.", "2.", "3." }, markers.ConvertAll(m => m.Text));
        Assert.All(markers, m => Assert.Equal(0, m.X, 3));

        var text = PageText(builder);
        Assert.True(text.IndexOf("Alpha", StringComparison.Ordinal) >= 0);
        Assert.True(
            text.IndexOf("Alpha", StringComparison.Ordinal) < text.IndexOf("Beta", StringComparison.Ordinal),
            "items retain their order");
    }

    [Fact]
    public void OrderedList_WrappedItemContent_AlignsAtHangingIndent()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(120), Unit.FromPoint(400));
        section.Margin = Unit.FromPoint(0);

        var list = section.Blocks.AddList(ListStyle.Number);
        list.HangingIndent = Unit.FromPoint(20);
        list.AddItem("Wrapping content that spills onto a second line");

        var draws = TextDraws(builder);

        var marker = draws.Find(d => d.Text == "1.");
        Assert.Equal(0, marker.X, 3);

        // The item wraps, and every content line lands at the hanging indent, never under the marker.
        var contentLines = draws.FindAll(d => d.Text != "1." && d.Text.Length > 0);
        Assert.True(contentLines.Count >= 2, "content wrapped to at least two lines");
        Assert.All(contentLines, line => Assert.Equal(20, line.X, 3));
    }
}

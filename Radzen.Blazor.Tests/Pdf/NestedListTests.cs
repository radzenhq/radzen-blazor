#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class NestedListTests
{
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

    private static Section AddSection(Document document)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));
        return section;
    }

    [Fact]
    public void NestedBulletList_IndentsInnerItems_WithOwnMarkers()
    {
        var document = new Document();
        var section = AddSection(document);

        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.HangingIndent = Unit.FromPoint(20);
        var first = list.AddItem("Outer one");
        list.AddItem("Outer two");

        var nested = first.AddList(ListStyle.Bullet);
        nested.HangingIndent = Unit.FromPoint(15);
        nested.AddItem("Inner one");
        nested.AddItem("Inner two");

        var draws = TextDraws(document);
        var texts = draws.ConvertAll(d => d.Text);

        Assert.Equal(
            new[] { "Outer one", "Inner one", "Inner two", "Outer two" },
            texts.FindAll(t => t.StartsWith("Outer", StringComparison.Ordinal) || t.StartsWith("Inner", StringComparison.Ordinal)));

        var outerMarkers = draws.FindAll(d => Math.Abs(d.X) < 1e-3 && d.Text.Length > 0);
        Assert.Equal(2, outerMarkers.Count);
        var innerMarkers = draws.FindAll(d => Math.Abs(d.X - 20) < 1e-3 && d.Text.Length > 0 && !d.Text.StartsWith("Outer", StringComparison.Ordinal));
        Assert.Equal(2, innerMarkers.Count);
        Assert.Equal(outerMarkers[0].Text, innerMarkers[0].Text);

        Assert.Equal(20, draws.Find(d => d.Text == "Outer one").X, 3);
        Assert.Equal(35, draws.Find(d => d.Text == "Inner one").X, 3);
        Assert.Equal(35, draws.Find(d => d.Text == "Inner two").X, 3);
        Assert.Equal(20, draws.Find(d => d.Text == "Outer two").X, 3);
    }

    [Fact]
    public void NestedOrderedList_NumbersIndependently_AndIndents()
    {
        var document = new Document();
        var section = AddSection(document);

        var list = section.Blocks.AddList(ListStyle.Number);
        list.HangingIndent = Unit.FromPoint(24);
        list.AddItem("Alpha");
        var second = list.AddItem("Beta");
        list.AddItem("Gamma");

        var nested = second.AddList(ListStyle.Number);
        nested.HangingIndent = Unit.FromPoint(18);
        nested.AddItem("Sub one");
        nested.AddItem("Sub two");
        nested.AddItem("Sub three");

        var draws = TextDraws(document);

        var outerMarkers = draws.FindAll(d => Math.Abs(d.X) < 1e-3 && d.Text is "1." or "2." or "3.");
        Assert.Equal(new[] { "1.", "2.", "3." }, outerMarkers.ConvertAll(m => m.Text));

        var innerMarkers = draws.FindAll(d => Math.Abs(d.X - 24) < 1e-3 && d.Text is "1." or "2." or "3.");
        Assert.Equal(new[] { "1.", "2.", "3." }, innerMarkers.ConvertAll(m => m.Text));

        Assert.Equal(42, draws.Find(d => d.Text == "Sub one").X, 3);
        Assert.Equal(24, draws.Find(d => d.Text == "Gamma").X, 3);
    }

    [Fact]
    public void FlatLists_OutputBytes_MatchPreNestingBaseline()
    {
        var document = new Document();
        var section = AddSection(document);

        var bullets = section.Blocks.AddList(ListStyle.Bullet);
        bullets.HangingIndent = Unit.FromPoint(20);
        bullets.AddItem("Alpha");
        bullets.AddItem("Beta");

        var numbers = section.Blocks.AddList(ListStyle.Number);
        numbers.LeftIndent = Unit.FromPoint(6);
        numbers.AddItem("One");
        numbers.AddItem("Two");
        numbers.AddItem("Three");

        using var stream = new MemoryStream();
        new DocumentRenderer().SaveToStream(document, stream);

        Assert.Equal(
            "52508AF33626BD972F78F37217ED1F2C8CCB38ECCD2FCC74A0D56CEFBB4DD09D",
            Convert.ToHexString(SHA256.HashData(stream.ToArray())));
    }
}

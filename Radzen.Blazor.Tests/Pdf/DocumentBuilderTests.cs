#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentBuilderTests
{
    [Fact]
    public void DocumentInfo_DefaultsNull()
    {
        var info = new DocumentBuilder().Info;
        Assert.Null(info.Title);
        Assert.Null(info.Author);
        Assert.Null(info.Subject);
        Assert.Null(info.Keywords);
        Assert.Null(info.Creator);
    }

    [Fact]
    public void DocumentInfo_PropertiesSettable()
    {
        var info = new DocumentBuilder().Info;
        info.Title = "T";
        info.Author = "A";
        info.Subject = "S";
        info.Keywords = "K";
        info.Creator = "C";
        Assert.Equal("T", info.Title);
        Assert.Equal("A", info.Author);
        Assert.Equal("S", info.Subject);
        Assert.Equal("K", info.Keywords);
        Assert.Equal("C", info.Creator);
    }

    [Fact]
    public void Sections_IsReadOnlyList()
    {
        var doc = new DocumentBuilder();
        Assert.IsAssignableFrom<IReadOnlyList<Section>>(doc.Sections);
        Assert.Empty(doc.Sections);
    }

    [Fact]
    public void Sections_AddReturnsSectionAndAppends()
    {
        var doc = new DocumentBuilder();
        var s1 = doc.Sections.Add();
        var s2 = doc.Sections.Add();
        Assert.NotNull(s1);
        Assert.Equal(2, doc.Sections.Count);
        Assert.Same(s1, doc.Sections[0]);
        Assert.Same(s2, doc.Sections[1]);
    }

    [Fact]
    public void Section_PageDefaults()
    {
        var s = new DocumentBuilder().Sections.Add();
        Assert.Equal(PageSizes.A4, s.PageSize);
        Assert.Equal(PageOrientation.Portrait, s.Orientation);
        Assert.Equal(FlowDirection.LeftToRight, s.Direction);
        Assert.Equal(WritingMode.HorizontalTopToBottom, s.WritingMode);
    }

    [Fact]
    public void Section_MarginsDefaultTwoPointFiveCentimeters()
    {
        var s = new DocumentBuilder().Sections.Add();
        var expected = Unit.FromCentimeter(2.5).Point;
        Assert.Equal(expected, s.Margins.Top.Point, 9);
        Assert.Equal(expected, s.Margins.Right.Point, 9);
        Assert.Equal(expected, s.Margins.Bottom.Point, 9);
        Assert.Equal(expected, s.Margins.Left.Point, 9);
    }

    [Fact]
    public void Section_MarginConvenienceSetsAllEdges()
    {
        var s = new DocumentBuilder().Sections.Add();
        s.Margin = Unit.FromPoint(18);
        Assert.Equal(18, s.Margins.Top.Point, 9);
        Assert.Equal(18, s.Margins.Right.Point, 9);
        Assert.Equal(18, s.Margins.Bottom.Point, 9);
        Assert.Equal(18, s.Margins.Left.Point, 9);
    }

    [Fact]
    public void Section_PropertiesSettable()
    {
        var s = new DocumentBuilder().Sections.Add();
        s.PageSize = PageSizes.Letter;
        s.Orientation = PageOrientation.Landscape;
        s.Direction = FlowDirection.RightToLeft;
        s.WritingMode = WritingMode.VerticalRightToLeft;
        Assert.Equal(PageSizes.Letter, s.PageSize);
        Assert.Equal(PageOrientation.Landscape, s.Orientation);
        Assert.Equal(FlowDirection.RightToLeft, s.Direction);
        Assert.Equal(WritingMode.VerticalRightToLeft, s.WritingMode);
    }

    [Fact]
    public void HeaderFooter_BlocksUsableLikeSectionBlocks()
    {
        var s = new DocumentBuilder().Sections.Add();
        var p = s.Header.Blocks.Add("page header");
        Assert.Single(s.Header.Blocks);
        Assert.Same(p, s.Header.Blocks[0]);
        Assert.Equal("page header", p.Text);

        var fp = s.Footer.Blocks.AddParagraph("foot");
        Assert.Single(s.Footer.Blocks);
        Assert.Same(fp, s.Footer.Blocks[0]);
    }
}

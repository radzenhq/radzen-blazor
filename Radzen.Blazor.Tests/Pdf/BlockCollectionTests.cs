#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class BlockCollectionTests
{
    private static BlockCollection NewBlocks() => new DocumentBuilder().Sections.Add().Blocks;

    [Fact]
    public void Blocks_IsReadOnlyListOfBlock()
    {
        var blocks = NewBlocks();
        Assert.IsAssignableFrom<IReadOnlyList<Block>>(blocks);
        Assert.Empty(blocks);
    }

    [Fact]
    public void AddString_CreatesParagraphWithText()
    {
        var blocks = NewBlocks();
        var p = blocks.Add("hello");
        Assert.IsType<Paragraph>(p);
        Assert.Equal("hello", p.Text);
        Assert.Single(blocks);
        Assert.Same(p, blocks[0]);
    }

    [Fact]
    public void AddGeneric_ReturnsSameInstanceAndAppends()
    {
        var blocks = NewBlocks();
        var table = new Table();
        var returned = blocks.Add(table);
        Assert.Same(table, returned);
        Assert.Same(table, blocks[0]);
    }

    [Fact]
    public void AddParagraph_WithAndWithoutText()
    {
        var blocks = NewBlocks();
        var empty = blocks.AddParagraph();
        var withText = blocks.AddParagraph("x");
        Assert.Null(empty.Text);
        Assert.Equal("x", withText.Text);
        Assert.Equal(2, blocks.Count);
    }

    [Fact]
    public void AddTable_AddsAndReturnsTable()
    {
        var blocks = NewBlocks();
        var t = blocks.AddTable();
        Assert.IsType<Table>(t);
        Assert.Same(t, blocks[0]);
    }

    [Fact]
    public void AddPageBreak_AddsAndReturnsPageBreak()
    {
        var blocks = NewBlocks();
        var pb = blocks.AddPageBreak();
        Assert.IsType<PageBreak>(pb);
        Assert.Same(pb, blocks[0]);
    }

    [Fact]
    public void AddImage_AddsAndReturnsImage()
    {
        var blocks = NewBlocks();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var img = blocks.AddImage(stream);
        Assert.IsType<Image>(img);
        Assert.Same(img, blocks[0]);
    }

    [Fact]
    public void Add_PreservesInsertionOrder()
    {
        var blocks = NewBlocks();
        var a = blocks.AddParagraph("a");
        var b = blocks.AddTable();
        var c = blocks.AddPageBreak();
        Assert.Same(a, blocks[0]);
        Assert.Same(b, blocks[1]);
        Assert.Same(c, blocks[2]);
    }

    [Fact]
    public void Add_SameInstanceTwice_Throws()
    {
        var blocks = NewBlocks();
        var p = new Paragraph();
        blocks.Add(p);
        Assert.Throws<ArgumentException>(() => blocks.Add(p));
    }

    [Fact]
    public void Add_Null_Throws()
    {
        var blocks = NewBlocks();
        Assert.Throws<ArgumentNullException>(() => blocks.Add<Block>(null!));
    }

    [Fact]
    public void AddImage_Null_Throws()
    {
        var blocks = NewBlocks();
        Assert.Throws<ArgumentNullException>(() => blocks.AddImage(null!));
    }

    [Fact]
    public void Image_DefaultsNaturalSize()
    {
        var blocks = NewBlocks();
        using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
        var img = blocks.AddImage(stream);
        Assert.Null(img.Width);
        Assert.Null(img.Height);
    }

    [Fact]
    public void Image_WidthHeightSettable()
    {
        var blocks = NewBlocks();
        using var stream = new MemoryStream(new byte[] { 1 });
        var img = blocks.AddImage(stream);
        img.Width = Unit.FromPoint(100);
        img.Height = Unit.FromPoint(50);
        Assert.Equal(100, img.Width!.Value.Point, 9);
        Assert.Equal(50, img.Height!.Value.Point, 9);
    }

    [Fact]
    public void Image_DataBufferedAndSurvivesSourceDisposal()
    {
        var blocks = NewBlocks();
        var payload = new byte[] { 10, 20, 30, 40 };
        Image img;
        using (var stream = new MemoryStream(payload))
        {
            img = blocks.AddImage(stream);
        }
        Assert.Equal(payload, img.Data);
    }
}

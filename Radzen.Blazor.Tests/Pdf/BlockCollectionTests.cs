#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class BlockCollectionTests
{
    private static BlockCollection NewBlocks() => new Document().Sections.Add().Blocks;

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
    public void AddContainer_CreatesOwnedContainer()
    {
        var blocks = NewBlocks();

        var container = blocks.AddContainer();

        Assert.Same(container, Assert.Single(blocks));
        Assert.True(blocks.StructureChanged);
        Assert.Throws<InvalidOperationException>(() => NewBlocks().Add(container));
    }

    [Fact]
    public void Add_SameInstanceTwice_Throws()
    {
        var blocks = NewBlocks();
        var p = new Paragraph();
        blocks.Add(p);
        Assert.Throws<InvalidOperationException>(() => blocks.Add(p));
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

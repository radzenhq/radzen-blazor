#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ListFontCascadeTests
{
    [Fact]
    public void SectionLevelList_InheritsNormalFontSize()
    {
        var builder = new DocumentBuilder();
        builder.Styles.Normal.Font.Size = 20;
        var section = builder.Sections.Add();
        section.Blocks.AddList().AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void SectionLevelList_InheritsNormalFontFamily()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        builder.Styles.Normal.Font.Name = BuildTestSupport.Latin;
        var section = builder.Sections.Add();
        section.Blocks.AddList().AddItem("Hello");

        var reader = BuildTestSupport.Read(builder);

        Assert.Single(BuildTestSupport.Type0Fonts(reader));
    }

    [Fact]
    public void ListInsideCell_InheritsCellFontSize()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Font.Size = 16;
        cell.Blocks.AddList().AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(16.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void PdfA3B_ListWithDefaultFont_SavesReferencingEmbeddedFont()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(builder);
        builder.Styles.Normal.Font.Name = BuildTestSupport.Latin;
        var section = builder.Sections.Add();
        section.Blocks.AddList().AddItem("Embedded item");

        var exception = Record.Exception(() => builder.ToArray());
        Assert.Null(exception);

        var reader = BuildTestSupport.Read(builder);
        var fonts = BuildTestSupport.Fonts(reader);
        Assert.NotEmpty(fonts);
        Assert.All(fonts, font => Assert.Equal("Type0", BuildTestSupport.Name(reader, font, "Subtype")));

        var leaves = BuildTestSupport.PageLeaves(reader);
        var content = Encoding.Latin1.GetString(BuildTestSupport.Content(reader, leaves[0].Page));
        Assert.DoesNotContain("Helvetica", content, StringComparison.Ordinal);

        Assert.Contains("Embedded item", BuildTestSupport.Reload(builder).Pages[0].ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitItemFont_WinsOverNormal()
    {
        var builder = new DocumentBuilder();
        builder.Styles.Normal.Font.Size = 20;
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList();
        list.AddItem("Item").Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void ExplicitListFont_WinsOverNormal()
    {
        var builder = new DocumentBuilder();
        builder.Styles.Normal.Font.Size = 20;
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList();
        list.Font.Size = 14;
        list.AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(builder));

        Assert.Contains(14.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
    }
}

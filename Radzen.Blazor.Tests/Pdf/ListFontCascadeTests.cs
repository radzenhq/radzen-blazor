#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ListFontCascadeTests
{
    [Fact]
    public void SectionLevelList_InheritsNormalFontSize()
    {
        var document = new Document();
        document.Styles.Normal.Font.Size = 20;
        var section = document.Sections.Add();
        section.Blocks.AddList().AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void SectionLevelList_InheritsNormalFontFamily()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;
        var section = document.Sections.Add();
        section.Blocks.AddList().AddItem("Hello");

        var builderRenderer = new DocumentRenderer();
        var reader = BuildTestSupport.Read(document, builderRenderer);

        Assert.Single(BuildTestSupport.Type0Fonts(reader));
    }

    [Fact]
    public void ListInsideCell_InheritsCellFontSize()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var cell = table.Rows.Add().Cells[0];
        cell.Font.Size = 16;
        cell.Blocks.AddList().AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(16.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void PdfA3B_ListWithDefaultFont_SavesReferencingEmbeddedFont()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;
        var section = document.Sections.Add();
        section.Blocks.AddList().AddItem("Embedded item");

        var exception = Record.Exception(() => builderRenderer.ToArray(document));
        Assert.Null(exception);

        var reader = BuildTestSupport.Read(document, builderRenderer);
        var fonts = BuildTestSupport.Fonts(reader);
        Assert.NotEmpty(fonts);
        Assert.All(fonts, font => Assert.Equal("Type0", BuildTestSupport.Name(reader, font, "Subtype")));

        Assert.All(
            fonts,
            font => Assert.NotEqual("Helvetica", BuildTestSupport.Name(reader, font, "BaseFont")));

        Assert.Contains("Embedded item", BuildTestSupport.Reload(document, builderRenderer).Pages[0].ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitItemFont_WinsOverNormal()
    {
        var document = new Document();
        document.Styles.Normal.Font.Size = 20;
        var section = document.Sections.Add();
        var list = section.Blocks.AddList();
        list.AddItem("Item").Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void ExplicitListFont_WinsOverNormal()
    {
        var document = new Document();
        document.Styles.Normal.Font.Size = 20;
        var section = document.Sections.Add();
        var list = section.Blocks.AddList();
        list.Font.Size = 14;
        list.AddItem("Item");

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(14.0, sizes);
        Assert.DoesNotContain(20.0, sizes);
    }
}

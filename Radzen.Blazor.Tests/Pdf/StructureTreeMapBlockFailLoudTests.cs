#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class StructureTreeMapBlockFailLoudTests
{
    private sealed class UnmappedBlock : Block;

    [Fact]
    public void Build_UnmappedBlockType_ThrowsNamingTheType()
    {
        var document = new Document { Language = "en" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Blocks.Add(new UnmappedBlock());

        var ex = Assert.Throws<NotSupportedException>(() => builderRenderer.ToArray(document));
        Assert.Contains(typeof(UnmappedBlock).FullName!, ex.Message);
    }

    [Fact]
    public void Build_SupportedBlockTypes_DoNotThrow()
    {
        var document = new Document { Language = "en" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();

        var heading = BuildTestSupport.AddText(section, "Title", BuildTestSupport.Latin, 18);
        heading.StyleName = "Heading1";
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);

        var table = section.Blocks.Add(new Table());
        table.Columns.Add();
        var row = table.Rows.Add();
        row.IsHeaderRow = true;
        TableLayoutSupport.Fill(row.Cells[0], "Cell");

        var list = new ListBlock();
        var item = list.Items.Add();
        item.Font.Family = BuildTestSupport.Latin;
        item.Inlines.Add("One").Font.Family = BuildTestSupport.Latin;
        section.Blocks.Add(list);

        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg"))).AlternateText = "An image";

        var bytes = builderRenderer.ToArray(document);
        Assert.NotEmpty(bytes);
    }
}

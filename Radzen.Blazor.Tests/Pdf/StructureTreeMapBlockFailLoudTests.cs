#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// StructureTreeBuilder.MapBlock must fail loud on a block type it does not map
// rather than silently dropping it from the tagged / PDF-UA structure tree.
public class StructureTreeMapBlockFailLoudTests
{
    private sealed class UnmappedBlock : Block;

    [Fact]
    public void Build_UnmappedBlockType_ThrowsNamingTheType()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en" };
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        section.Blocks.Add(new UnmappedBlock());

        var ex = Assert.Throws<NotSupportedException>(() => builder.ToArray());
        Assert.Contains(typeof(UnmappedBlock).FullName!, ex.Message);
    }

    [Fact]
    public void Build_SupportedBlockTypes_DoNotThrow()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en" };
        builder.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();

        var heading = BuildTestSupport.AddText(section, "Title", BuildTestSupport.Latin, 18);
        heading.StyleName = "Heading1";
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin);

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.IsHeader = true;
        TableLayoutSupport.Fill(row.Cells[0], "Cell");

        var list = new List();
        var item = list.AddItem();
        item.Font.Name = BuildTestSupport.Latin;
        item.Inlines.Add("One").Font.Name = BuildTestSupport.Latin;
        section.Blocks.Add(list);

        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg")).AlternateText = "An image";

        var bytes = builder.ToArray();
        Assert.NotEmpty(bytes);
    }
}

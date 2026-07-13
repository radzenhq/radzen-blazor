#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedContentTests
{
    [Fact]
    public void TaggedFigure_EmitsAltText()
    {
        var builder = new DocumentBuilder
        {
            PdfUA = true,
            Language = "en-US",
        };
        builder.Info.Title = "Alt test";
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "A red square";

        var reader = BuildTestSupport.Read(builder);
        var figure = StructureTestHelpers.FindElement(reader, "Figure");
        Assert.NotNull(figure);
        Assert.Equal("A red square",
            Assert.IsType<StringObject>(reader.Resolve(figure!["Alt"]!)).Value);
    }

    [Fact]
    public void TaggedList_BuildsLListItemLabelAndBody()
    {
        var builder = new DocumentBuilder
        {
            PdfUA = true,
            Language = "en-US",
        };
        builder.Info.Title = "List test";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = BuildTestSupport.Latin;
        list.Font.Size = 12;
        list.AddItem("First");
        list.AddItem("Second");

        var reader = BuildTestSupport.Read(builder);
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);

        Assert.Contains("L", types);
        Assert.Equal(2, types.FindAll(t => t == "LI").Count);
        Assert.Equal(2, types.FindAll(t => t == "Lbl").Count);
        Assert.Equal(2, types.FindAll(t => t == "LBody").Count);
    }

    [Fact]
    public void UntaggedList_StaysUntagged_WhenNotPdfUA()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.AddItem("First");
        list.AddItem("Second");

        var reader = BuildTestSupport.Read(builder);
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);

        Assert.DoesNotContain("L", types);
        Assert.DoesNotContain("LBody", types);
    }
}

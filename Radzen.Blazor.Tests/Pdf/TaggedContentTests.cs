#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedContentTests
{
    [Fact]
    public void TaggedFigure_EmitsAltText()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Alt test";
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "A red square";

        var reader = BuildTestSupport.Read(document, builderRenderer);
        var figure = StructureTestHelpers.FindElement(reader, "Figure");
        Assert.NotNull(figure);
        Assert.Equal("A red square",
            Assert.IsType<StringObject>(reader.Resolve(figure!["Alt"]!)).Value);
    }

    [Fact]
    public void TaggedList_BuildsLListItemLabelAndBody()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "List test";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Family = BuildTestSupport.Latin;
        list.Font.Size = 12;
        list.AddItem("First");
        list.AddItem("Second");

        var reader = BuildTestSupport.Read(document, builderRenderer);
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
        var document = new Document();
        var section = document.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.AddItem("First");
        list.AddItem("Second");

        var builderRenderer = new DocumentRenderer();
        var reader = BuildTestSupport.Read(document, builderRenderer);
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);

        Assert.DoesNotContain("L", types);
        Assert.DoesNotContain("LBody", types);
    }
}

#nullable enable
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedContentTests
{
    private static string Rendered(Document document, DocumentRenderer renderer)
        => Encoding.Latin1.GetString(renderer.ToArray(document));

    private static string Element(string type) => $"/Type /StructElem /S /{type} /P ";

    private static void ElementCount(string emission, string type, int expected)
    {
        var actual = Regex.Matches(emission, Regex.Escape(Element(type))).Count;
        Assert.True(
            actual == expected,
            $"Expected {expected} '/S /{type}' structure elements, found {actual}.\n{Excerpt(emission)}");
    }

    [Fact]
    public void TaggedFigure_EmitsAltText()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Alt test";
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "A red square";

        var emission = Rendered(document, new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        Carries("figure element", "/Alt (A red square)", Line(emission, Element("Figure")));
    }

    [Fact]
    public void TaggedList_BuildsLListItemLabelAndBody()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "List test";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Family = BuildTestSupport.Latin;
        list.Font.Size = 12;
        list.AddItem("First");
        list.AddItem("Second");

        var emission = Rendered(document, new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        Carries("emission", Element("L"), emission);
        ElementCount(emission, "LI", 2);
        ElementCount(emission, "Lbl", 2);
        ElementCount(emission, "LBody", 2);
    }

    [Fact]
    public void UntaggedList_StaysUntagged_WhenNotPdfUA()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.AddItem("First");
        list.AddItem("Second");

        var emission = Rendered(document, new DocumentRenderer());

        Lacks("emission", Element("L"), emission);
        Lacks("emission", Element("LBody"), emission);
    }
}

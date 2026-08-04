#nullable enable
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class HeadingLevelStructureTests
{
    private static string Structure(Document document)
    {
        var emission = Emit(new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(document));
        var root = IndirectObject(
            emission,
            Shaped("catalog", @"/StructTreeRoot (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value);

        Shaped("structure tree root", @"/K (\d+) 0 R", root);
        return emission;
    }

    private static void ElementCount(string emission, string type, int expected)
    {
        var fragment = $"/Type /StructElem /S /{type} /P ";
        var count = BuildTestSupport.CountOccurrences(emission, fragment);
        Assert.True(
            count == expected,
            $"Expected {expected} structure elements carrying '{fragment}', found {count}.\n{Excerpt(emission)}");
    }

    private static Document Authored(string styleName, int? headingLevel, bool declare)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Headings";
        BuildTestSupport.RegisterLatin(document);

        if (declare)
        {
            document.Styles.Add(styleName).HeadingLevel = headingLevel;
        }

        var paragraph = BuildTestSupport.AddText(document.Sections.Add(), "Title", BuildTestSupport.Latin);
        paragraph.StyleName = styleName;
        return document;
    }

    [Fact]
    public void StyleWithHeadingLevel_MapsToTheMatchingHeadingElement()
        => ElementCount(Structure(Authored("Lead", 2, declare: true)), "H2", 1);

    [Fact]
    public void BuiltInHeadingStyle_KeepsMappingToItsLevel()
        => ElementCount(Structure(Authored("Heading4", null, declare: false)), "H4", 1);

    [Fact]
    public void HeadingLevel_IsInheritedFromTheBaseStyleChain()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Headings";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("ChapterTitle", "Heading1");

        var paragraph = BuildTestSupport.AddText(document.Sections.Add(), "Title", BuildTestSupport.Latin);
        paragraph.StyleName = "ChapterTitle";

        ElementCount(Structure(document), "H1", 1);
    }

    [Fact]
    public void StyleNamedLikeAHeadingWithoutTheLevel_IsAnOrdinaryParagraph()
    {
        var emission = Structure(Authored("H1", null, declare: true));

        ElementCount(emission, "H1", 0);
        ElementCount(emission, "P", 1);
    }
}

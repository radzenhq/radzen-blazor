#nullable enable
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class HeadingLevelStructureTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static ProbeElement Structure(Document document)
        => TaggedStructureProbe.Root(BuildTestSupport.Read(document, Accessible()));

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
        => Assert.Equal("H2", TaggedStructureProbe.Single(Structure(Authored("Lead", 2, declare: true)), "H2").Type);

    [Fact]
    public void BuiltInHeadingStyle_KeepsMappingToItsLevel()
        => Assert.Equal("H4", TaggedStructureProbe.Single(Structure(Authored("Heading4", null, declare: false)), "H4").Type);

    [Fact]
    public void HeadingLevel_IsInheritedFromTheBaseStyleChain()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Headings";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("ChapterTitle", "Heading1");

        var paragraph = BuildTestSupport.AddText(document.Sections.Add(), "Title", BuildTestSupport.Latin);
        paragraph.StyleName = "ChapterTitle";

        Assert.Equal("H1", TaggedStructureProbe.Single(Structure(document), "H1").Type);
    }

    [Fact]
    public void StyleNamedLikeAHeadingWithoutTheLevel_IsAnOrdinaryParagraph()
    {
        var root = Structure(Authored("H1", null, declare: true));

        Assert.Empty(TaggedStructureProbe.All(root, "H1"));
        Assert.Single(TaggedStructureProbe.All(root, "P"));
    }
}

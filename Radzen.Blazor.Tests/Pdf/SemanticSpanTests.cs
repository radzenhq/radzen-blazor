#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class SemanticSpanTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static Document Authored(Action<Run> author)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Span";
        BuildTestSupport.RegisterLatin(document);

        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("The motto is ").Font.Family = BuildTestSupport.Latin;
        var run = paragraph.Inlines.Add("carpe diem");
        run.Font.Family = BuildTestSupport.Latin;
        author(run);
        paragraph.Inlines.Add(" indeed.").Font.Family = BuildTestSupport.Latin;

        return document;
    }

    private static IEnumerable<SemanticStructureNode> Spans(Document document)
        => DocumentLayouter.Layout(document).Semantics.Structure.Nodes
            .Where(node => node.Intent == SemanticIntent.Span);

    private static List<string> Types(DocumentReader reader)
    {
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);
        return types;
    }

    [Fact]
    public void RunWithoutRoleOrLanguage_IsContentOfItsParagraph()
        => Assert.Empty(Spans(Authored(static _ => { })));

    [Fact]
    public void RunWithLanguage_BecomesASpanCarryingThatLanguage()
    {
        var span = Assert.Single(Spans(Authored(static run => run.Language = "la")));

        Assert.Equal("la", span.Language);
        Assert.Null(span.Role);
        Assert.False(span.RoleIsDeclared);
    }

    [Fact]
    public void RunWithRole_BecomesASpanCarryingThatRole()
    {
        var span = Assert.Single(Spans(Authored(static run => run.Role = "Quote")));

        Assert.Equal("Quote", span.Role);
        Assert.True(span.RoleIsDeclared);
        Assert.Null(span.Language);
    }

    [Fact]
    public void SpanWithoutARole_IsTaggedAsSpan()
    {
        var reader = BuildTestSupport.Read(Authored(static run => run.Language = "la"), Accessible());

        Assert.Contains("Span", Types(reader));
    }

    [Fact]
    public void SpanRole_NamingAStandardStructureType_IsTaggedWithIt()
    {
        var reader = BuildTestSupport.Read(Authored(static run => run.Role = "Quote"), Accessible());
        var types = Types(reader);

        Assert.Contains("Quote", types);
        Assert.DoesNotContain("Span", types);
    }

    [Fact]
    public void SpanRole_OutsideTheRoleMap_IsRejected()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => Accessible().ToArray(Authored(static run => run.Role = "Motto")));

        Assert.Contains("Motto", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SpanRole_DeclaredInTheRoleMap_IsTaggedAndMapped()
    {
        var renderer = Accessible();
        renderer.RoleMap.Add("Motto", "Span");
        var reader = DocumentReader.Parse(
            renderer.ToArray(Authored(static run => run.Role = "Motto")));

        Assert.Contains("Motto", Types(reader));
    }

    [Fact]
    public void SpanLanguage_IsWrittenAsLangOnTheStructureElement()
    {
        var reader = BuildTestSupport.Read(Authored(static run => run.Language = "la"), Accessible());
        var span = Assert.IsType<DictionaryObject>(StructureTestHelpers.FindElement(reader, "Span"));

        Assert.Equal("la", Assert.IsType<StringObject>(reader.Resolve(span["Lang"])).Value);
    }

    [Fact]
    public void ParagraphElements_CarryNoLanguageOfTheirOwn()
    {
        var reader = BuildTestSupport.Read(Authored(static run => run.Language = "la"), Accessible());
        var paragraph = Assert.IsType<DictionaryObject>(StructureTestHelpers.FindElement(reader, "P"));

        Assert.False(paragraph.ContainsKey("Lang"));
    }

    [Fact]
    public void LinkedRunWithALanguage_NestsItsSpanInsideTheLink()
    {
        var document = Authored(static run =>
        {
            run.Language = "la";
            run.Link = "https://www.radzen.com";
        });

        var reader = BuildTestSupport.Read(document, Accessible());
        var types = Types(reader);

        Assert.Equal(["Document", "Sect", "P", "Link", "Span"], types);
    }

    [Fact]
    public void InlineImageWithALanguage_CarriesItOnItsFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Figure";
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Logo ").Font.Family = BuildTestSupport.Latin;
        var picture = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        picture.AlternateText = "Radzen";
        picture.Language = "bg";

        var reader = BuildTestSupport.Read(document, Accessible());
        var figure = Assert.IsType<DictionaryObject>(StructureTestHelpers.FindElement(reader, "Figure"));

        Assert.Equal("bg", Assert.IsType<StringObject>(reader.Resolve(figure["Lang"])).Value);
    }

    [Fact]
    public void EmptyRole_IsRejected()
        => Assert.Throws<ArgumentException>(() => new Paragraph().Inlines.Add("Text").Role = "");

    [Fact]
    public void EmptyLanguage_IsRejected()
        => Assert.Throws<ArgumentException>(() => new Paragraph().Inlines.Add("Text").Language = "");
}

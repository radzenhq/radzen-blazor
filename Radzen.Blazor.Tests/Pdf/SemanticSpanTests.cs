#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string RootElement(string emission)
        => Shaped("StructTreeRoot", @"/K (\d+) 0 R", Line(emission, "/Type /StructTreeRoot")).Groups[1].Value;

    private static string TypeOf(string emission, string number)
        => Shaped(
            $"structure element {number} 0 R",
            @"/Type /StructElem /S /(\w+) /P ",
            IndirectObject(emission, number)).Groups[1].Value;

    private static string? OnlyElementKid(string emission, string number)
    {
        var kids = Regex.Match(IndirectObject(emission, number), @"/K \[([^\]]*)\]");
        if (!kids.Success)
        {
            return null;
        }

        string[] references =
        [
            .. Regex.Matches(Regex.Replace(kids.Groups[1].Value, "<<.*?>>", " "), @"(\d+) 0 R")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value),
        ];

        return references.Length == 0 ? null : Assert.Single(references);
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
        var emission = Emit(Accessible().Render(Authored(static run => run.Language = "la")));

        Carries("tagged emission", StructureMarker("Span"), emission);
    }

    [Fact]
    public void SpanRole_NamingAStandardStructureType_IsTaggedWithIt()
    {
        var emission = Emit(Accessible().Render(Authored(static run => run.Role = "Quote")));

        Carries("tagged emission", StructureMarker("Quote"), emission);
        Lacks("tagged emission", StructureMarker("Span"), emission);
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

        var emission = Emit(renderer.Render(Authored(static run => run.Role = "Motto")));

        Carries("tagged emission", StructureMarker("Motto"), emission);
    }

    [Fact]
    public void SpanLanguage_IsWrittenAsLangOnTheStructureElement()
    {
        var emission = Emit(Accessible().Render(Authored(static run => run.Language = "la")));

        Carries("Span element", "/Lang (la)", StructureElement(emission, "Span"));
    }

    [Fact]
    public void ParagraphElements_CarryNoLanguageOfTheirOwn()
    {
        var emission = Emit(Accessible().Render(Authored(static run => run.Language = "la")));

        Lacks("P element", "/Lang", StructureElement(emission, "P"));
    }

    [Fact]
    public void LinkedRunWithALanguage_NestsItsSpanInsideTheLink()
    {
        var document = Authored(static run =>
        {
            run.Language = "la";
            run.Link = "https://www.radzen.com";
        });

        var emission = Emit(Accessible().Render(document));
        var types = new List<string>();

        for (string? number = RootElement(emission); number is not null; number = OnlyElementKid(emission, number))
        {
            types.Add(TypeOf(emission, number));
        }

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

        var emission = Emit(Accessible().Render(document));

        Carries("Figure element", "/Lang (bg)", StructureElement(emission, "Figure"));
    }

    [Fact]
    public void EmptyRole_IsRejected()
        => Assert.Throws<ArgumentException>(() => new Paragraph().Inlines.Add("Text").Role = "");

    [Fact]
    public void EmptyLanguage_IsRejected()
        => Assert.Throws<ArgumentException>(() => new Paragraph().Inlines.Add("Text").Language = "");
}

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Codes;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class TaggedFigureContentTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private readonly record struct Marked(string Tag, int? Mcid, List<string> Operators);

    private static List<Marked> MarkedContent(DocumentReader reader, int pageIndex)
    {
        var open = new Stack<Marked>();
        var result = new List<Marked>();
        foreach (var operation in ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, pageIndex)))
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    {
                        int? mcid = null;
                        for (var i = 1; i < operation.Operands.Count - 1; i++)
                        {
                            if (operation.Operands[i].Kind == ContentTokenKind.Name
                                && operation.Operands[i].Text == "MCID"
                                && operation.Operands[i + 1].Kind == ContentTokenKind.Number)
                            {
                                mcid = (int)operation.Operands[i + 1].Number;
                                break;
                            }
                        }

                        var entry = new Marked(
                            operation.Operands.Count > 0 ? operation.Operands[0].Text : "",
                            mcid,
                            []);
                        open.Push(entry);
                        result.Add(entry);
                        break;
                    }

                case "EMC":
                    if (open.Count > 0)
                    {
                        open.Pop();
                    }

                    break;

                default:
                    foreach (var entry in open)
                    {
                        entry.Operators.Add(operation.Operator);
                    }

                    break;
            }
        }

        return result;
    }

    private static Document InlineImage(string? alternateText, bool midSentence)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Inline";
        BuildTestSupport.RegisterLatin(document);

        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Before ").Font.Family = BuildTestSupport.Latin;
        var image = paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(20);
        image.Height = Unit.FromPoint(20);
        image.AlternateText = alternateText;
        if (midSentence)
        {
            paragraph.Inlines.Add(" after.").Font.Family = BuildTestSupport.Latin;
        }

        return document;
    }

    [Fact]
    public void InlineFigure_OwnsItsOwnMarkedContent()
    {
        var reader = BuildTestSupport.Read(InlineImage("The Radzen logo", midSentence: false), Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var figure = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Figure");

        var mcid = Assert.Single(figure.Mcids);
        Assert.Same(figure.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mcid));

        var marked = Assert.Single(MarkedContent(reader, 0).Where(entry => entry.Mcid == mcid));
        Assert.Equal("Figure", marked.Tag);
        Assert.Contains("Do", marked.Operators);
    }

    [Fact]
    public void InlineFigureInTheMiddleOfASentence_KeepsSemanticReadingOrder()
    {
        var reader = BuildTestSupport.Read(InlineImage("The Radzen logo", midSentence: true), Accessible());
        var root = TaggedStructureProbe.Root(reader);
        var paragraph = TaggedStructureProbe.Single(root, "P");
        var figure = TaggedStructureProbe.Single(root, "Figure");

        Assert.Equal(
            new[] { "P", "Figure", "P" },
            MarkedContent(reader, 0).Where(entry => entry.Mcid is not null).Select(entry => entry.Tag).ToArray());

        Assert.Equal(2, paragraph.Mcids.Count);
        var mcid = Assert.Single(figure.Mcids);
        Assert.Equal(paragraph.Mcids[0] + 1, mcid);
        Assert.Equal(mcid + 1, paragraph.Mcids[1]);

        Assert.Equal(
            new object[] { paragraph.Mcids[0], figure, paragraph.Mcids[1] },
            paragraph.Kids);
    }

    [Fact]
    public void DecorativeInlineImage_IsArtifactContentAndNotParagraphTagged()
    {
        var reader = BuildTestSupport.Read(InlineImage(null, midSentence: true), Accessible());

        Assert.Empty(TaggedStructureProbe.All(TaggedStructureProbe.Root(reader), "Figure"));

        var marked = MarkedContent(reader, 0);
        Assert.All(
            marked.Where(entry => entry.Operators.Contains("Do")),
            entry => Assert.Equal("Artifact", entry.Tag));
        Assert.DoesNotContain(marked, entry => entry.Tag == "P" && entry.Operators.Contains("Do"));
    }

    private static Document Code(string? alternateText)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        var barcode = document.Sections.Add().Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(160), Unit.FromPoint(40));
        barcode.Font.Family = BuildTestSupport.Latin;
        barcode.ShowText = true;
        barcode.AlternateText = alternateText;
        return document;
    }

    [Fact]
    public void BarcodeWithAlternateText_MarksItsModulesAndCaptionUnderTheFigure()
    {
        var reader = BuildTestSupport.Read(Code("Order RADZEN"), Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var figure = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Figure");

        var mcid = Assert.Single(figure.Mcids);
        Assert.Same(figure.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mcid));

        var marked = Assert.Single(MarkedContent(reader, 0).Where(entry => entry.Mcid == mcid));
        Assert.Equal("Figure", marked.Tag);
        Assert.Contains("f", marked.Operators);
        Assert.Contains(marked.Operators, op => op is "Tj" or "TJ");
    }

    [Fact]
    public void DecorativeBarcode_StaysArtifactContent()
    {
        var reader = BuildTestSupport.Read(Code(null), Accessible());

        Assert.Empty(TaggedStructureProbe.All(TaggedStructureProbe.Root(reader), "Figure"));
        Assert.All(
            MarkedContent(reader, 0),
            entry => Assert.Equal("Artifact", entry.Tag));
    }

    [Fact]
    public void BlockImageFigure_OwnsItsMarkedContent()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Block";
        var image = document.Sections.Add().Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.AlternateText = "A red square";

        var reader = BuildTestSupport.Read(document, Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var figure = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Figure");

        var mcid = Assert.Single(figure.Mcids);
        Assert.Same(figure.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mcid));
        Assert.Contains("Do", Assert.Single(MarkedContent(reader, 0).Where(entry => entry.Mcid == mcid)).Operators);
    }
}

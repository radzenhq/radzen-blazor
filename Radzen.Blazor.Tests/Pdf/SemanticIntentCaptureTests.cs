#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents.Codes;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class SemanticIntentCaptureTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static DocumentRenderer Tagged() => new() { Conformance = PdfAConformance.PdfA2A };

    private static List<string> Types(DocumentReader reader)
    {
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);
        return types;
    }

    private static void Elements(string emission, string type, int expected)
    {
        var marker = StructureMarker(type);
        var count = Regex.Matches(emission, Regex.Escape(marker)).Count;
        Assert.True(
            count == expected,
            $"Expected {expected} '{marker}' elements in the emission, found {count}.");
    }

    private static string[] ElementKids(string element)
    {
        var kids = Regex.Match(element, @"/K \[([^\]]*)\]");
        if (!kids.Success)
        {
            return [];
        }

        return
        [
            .. Regex.Matches(Regex.Replace(kids.Groups[1].Value, "<<.*?>>", " "), @"(\d+) 0 R")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value),
        ];
    }

    private static Document Chapters(out TableOfContents toc)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Navigation";
        BuildTestSupport.RegisterLatin(document);

        var front = document.Sections.Add();
        toc = front.Blocks.AddTableOfContents();
        toc.Font.Family = BuildTestSupport.Latin;
        toc.AddEntry("Chapter One", "ch1");
        toc.AddEntry("Chapter Two", "ch2", level: 1);

        foreach (var (anchor, text) in new[] { ("ch1", "Chapter one body"), ("ch2", "Chapter two body") })
        {
            var section = document.Sections.Add();
            var paragraph = section.Blocks.AddParagraph();
            var run = paragraph.Inlines.Add(text);
            run.Font.Family = BuildTestSupport.Latin;
            run.Anchor = anchor;
        }

        return document;
    }

    private static void CollectTypes(StructureElement element, List<string> types)
    {
        types.Add(element.Type);
        foreach (var child in element.Children)
        {
            CollectTypes(child, types);
        }
    }

    [Fact]
    public void TableOfContents_CapturesNavigationIntentForEveryEntry()
    {
        var document = Chapters(out var toc);

        var structure = DocumentLayouter.Layout(document).Semantics.Structure;
        var nodes = structure.Nodes;
        var navigation = Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.Navigation));

        Assert.Equal(toc.Entries.Count, navigation.Children.Length);
        Assert.All(navigation.Children, child =>
        {
            var entry = nodes[child];
            Assert.Equal(SemanticIntent.NavigationEntry, entry.Intent);
            Assert.Equal(SemanticIntent.CrossReference, nodes[Assert.Single(entry.Children)].Intent);
        });

        Assert.Equal(
            toc.Entries.Count,
            structure.Associations
                .Where(association => nodes[association.Element].Intent == SemanticIntent.CrossReference)
                .Select(association => association.Element)
                .Distinct()
                .Count());
    }

    [Fact]
    public void HeaderAndFooterContent_CapturesPaginationArtifactIntent()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Header.Blocks.AddParagraph().Inlines.Add("Header").Font.Family = BuildTestSupport.Latin;
        section.Footer.Blocks.AddParagraph().Inlines.Add("Footer").Font.Family = BuildTestSupport.Latin;
        section.Blocks.AddParagraph().Inlines.Add("Body").Font.Family = BuildTestSupport.Latin;

        var artifacts = DocumentLayouter.Layout(document).Semantics.Structure.Artifacts;

        Assert.NotEmpty(artifacts);
        Assert.All(artifacts, artifact => Assert.Equal(SemanticArtifactKind.Pagination, artifact.Kind));
    }

    [Fact]
    public void List_IsCapturedWithAListItemForEveryItem()
    {
        var document = new Document();
        var list = document.Sections.Add().Blocks.AddList(ListStyle.Bullet);
        list.AddItem("Item");

        var nodes = DocumentLayouter.Layout(document).Semantics.Structure.Nodes;
        var captured = Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.List));

        Assert.Equal(
            list.Items.Count,
            captured.Children.Count(child => nodes[child].Intent == SemanticIntent.ListItem));
    }

    [Fact]
    public void TableOfContents_MapsToTheIsoNavigationStructureWhenTagged()
    {
        var document = Chapters(out var toc);

        var emission = Emit(Tagged().Render(document));

        Elements(emission, "TOC", 1);
        Elements(emission, "TOCI", toc.Entries.Count);
        Elements(emission, "Reference", toc.Entries.Count);
        Elements(emission, "Link", toc.Entries.Count);
    }

    [Fact]
    public void TableOfContents_RendersItsEntryTextUnderTheNavigationStructure()
    {
        var emission = Emit(Tagged().Render(Chapters(out _)));

        foreach (var number in ElementKids(StructureElement(emission, "TOC")))
        {
            var entry = IndirectObject(emission, number);
            Carries($"TOC kid {number} 0 R", StructureMarker("TOCI"), entry);

            var referenceNumber = Assert.Single(ElementKids(entry));
            var reference = IndirectObject(emission, referenceNumber);
            Carries($"TOCI kid {referenceNumber} 0 R", StructureMarker("Reference"), reference);

            var linkNumber = Assert.Single(ElementKids(reference));
            var link = IndirectObject(emission, linkNumber);
            Carries($"Reference kid {linkNumber} 0 R", StructureMarker("Link"), link);
            Shaped($"link element {linkNumber} 0 R marked content", @"/K \[\d", link);
            Carries($"link element {linkNumber} 0 R", "/Type /OBJR", link);
        }
    }

    [Fact]
    public void UntaggedRenderer_MaterializesNoStructureTree()
    {
        var scene = DocumentLayouter.Layout(Chapters(out _));
        var tree = new StructureTreeBuilder(scene.Semantics, RenderRequest.From(new DocumentRenderer()));

        tree.Build();

        Assert.False(tree.TaggingActive);
        Assert.Null(tree.DocumentElement);
    }

    [Fact]
    public void TableOfContents_InUntaggedOutput_AddsNoStructure()
    {
        var document = Chapters(out _);

        var emission = Emit(new DocumentRenderer().Render(document));

        Lacks("untagged emission", StructureMarker("TOC"), emission);
        Lacks("untagged emission", StructureMarker("TOCI"), emission);
    }

    [Fact]
    public void QrCodeWithAlternateText_IsAMeaningfulFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "Scan for details";

        var emission = Emit(Accessible().Render(document));

        Carries("Figure element", "/Alt (Scan for details)", StructureElement(emission, "Figure"));
    }

    [Fact]
    public void DecorativeQrCode_StaysDecorative()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "";

        Lacks("tagged emission", StructureMarker("Figure"), Emit(Accessible().Render(document)));
    }

    private static SemanticStructureNode SingleFigure(Document document)
    {
        var nodes = DocumentLayouter.Layout(document).Semantics.Structure.Nodes;
        return Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.Figure));
    }

    [Fact]
    public void DecorativeQrCode_IsCapturedAsADecorativeFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "";

        var figure = SingleFigure(document);

        Assert.True(figure.IsDecorative);
        Assert.Null(figure.AlternateText);
    }

    [Fact]
    public void QrCodeWithAlternateText_IsCapturedAsAMeaningfulFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "Scan for details";

        var figure = SingleFigure(document);

        Assert.False(figure.IsDecorative);
        Assert.Equal("Scan for details", figure.AlternateText);
    }

    [Fact]
    public void DecorativeQrCode_IsDrawnAsArtifactContent()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "";

        var reader = BuildTestSupport.Read(document, Accessible());

        var fills = FillTags(reader);

        Assert.NotEmpty(fills);
        Assert.All(fills, tag => Assert.Equal("Artifact", tag));
    }

    private static List<string> FillTags(DocumentReader reader)
    {
        var stack = new List<string>();
        var tags = new List<string>();
        foreach (var operation in ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0)))
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    stack.Add(operation.Operands.Count > 0 ? operation.Operands[0].Text : "");
                    break;
                case "EMC":
                    stack.RemoveAt(stack.Count - 1);
                    break;
                case "f":
                    tags.Add(stack.Count > 0 ? stack[^1] : "");
                    break;
            }
        }

        return tags;
    }

    [Fact]
    public void InlineImageWithAlternateText_IsAFigureInsideItsParagraph()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Inline";
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Logo: ").Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg")).AlternateText = "The Radzen logo";

        var emission = Emit(Accessible().Render(document));

        Carries("Figure element", "/Alt (The Radzen logo)", StructureElement(emission, "Figure"));
    }

    [Fact]
    public void Container_GroupsItsBlocksUnderADivWhenTagged()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Boxed";
        BuildTestSupport.RegisterLatin(document);
        var container = document.Sections.Add().Blocks.Add(new Container { Padding = Unit.FromPoint(8) });
        container.Blocks.AddParagraph().Inlines.Add("BOXED").Font.Family = BuildTestSupport.Latin;

        var emission = Emit(Accessible().Render(document));

        Elements(emission, "Div", 1);

        var child = Assert.Single(ElementKids(StructureElement(emission, "Div")));
        Carries($"Div kid {child} 0 R", StructureMarker("P"), IndirectObject(emission, child));
    }

    [Fact]
    public void Container_AddsNoGroupingToUntaggedOutput()
    {
        var document = new Document();
        document.Info.Title = "Boxed";
        BuildTestSupport.RegisterLatin(document);
        var container = document.Sections.Add().Blocks.Add(new Container { Padding = Unit.FromPoint(8) });
        container.Blocks.AddParagraph().Inlines.Add("BOXED").Font.Family = BuildTestSupport.Latin;

        Lacks("untagged emission", StructureMarker("Div"), Emit(new DocumentRenderer().Render(document)));
    }

    private static Table SpanningTable(Document document, bool repeat, bool header)
    {
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(140));
        section.Margins.SetAll(Unit.FromPoint(20));

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(200));
        var first = table.Rows.Add();
        first.RepeatOnEveryPage = repeat;
        first.IsHeaderRow = header;
        TableLayoutSupport.Fill(first.Cells[0], "Column");
        for (var i = 0; i < 12; i++)
        {
            TableLayoutSupport.Fill(table.Rows.Add().Cells[0], $"row {i}");
        }

        return table;
    }

    private static Document SpanningDocument(bool repeat, bool header)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Rows";
        BuildTestSupport.RegisterLatin(document);
        SpanningTable(document, repeat, header);
        return document;
    }

    [Fact]
    public void RepeatedHeaderRow_ProducesASingleSemanticHeaderRegardlessOfRepetition()
    {
        var document = SpanningDocument(repeat: true, header: true);

        var pages = DocumentLayouter.Layout(document).Pages;
        var reader = BuildTestSupport.Read(document, Accessible());
        var types = Types(reader);

        Assert.True(pages.Length > 1);
        Assert.All(pages, page => Assert.True(Assert.Single(page.Body.Tables).Rows[0].IsHeader));
        Assert.Single(types.Where(type => type == "TH"));

        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var header = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "TH");
        var paragraph = Assert.Single(header.Children);
        var mark = Assert.Single(paragraph.Mcids);
        Assert.Same(paragraph.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, 0, mark));

        for (var page = 1; page < BuildTestSupport.PageLeaves(reader).Count; page++)
        {
            foreach (var (_, mcid) in TaggedStructureProbe.MarkedContentInOrder(reader, page))
            {
                Assert.NotSame(paragraph.Dict, TaggedStructureProbe.OwnerOfMcid(reader, structRoot, page, mcid));
            }
        }
    }

    [Fact]
    public void RepeatOnEveryPage_WithoutTheHeaderRole_ProducesNoHeaderCells()
    {
        var emission = Emit(Accessible().Render(SpanningDocument(repeat: true, header: false)));

        Lacks("tagged emission", StructureMarker("TH"), emission);
        Carries("tagged emission", StructureMarker("TD"), emission);
    }

    [Fact]
    public void HeaderRole_WithoutRepetition_DoesNotRepeatTheRow()
    {
        var document = SpanningDocument(repeat: false, header: true);

        var pages = DocumentLayouter.Layout(document).Pages;

        Assert.True(pages.Length > 1);
        Assert.All(pages, page => Assert.All(
            Assert.Single(page.Body.Tables).Rows,
            row => Assert.False(row.IsHeader)));
        Carries("tagged emission", StructureMarker("TH"), Emit(Accessible().Render(document)));
    }

    [Fact]
    public void DecorativeInlineImage_IsClassifiedByTheSnapshotNotTheRenderer()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Logo: ").Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg")).AlternateText = "";

        var artifacts = DocumentLayouter.Layout(document).Semantics.Structure.Artifacts;

        Assert.Equal(SemanticArtifactKind.Decorative, Assert.Single(artifacts).Kind);
    }

    [Fact]
    public void InlineImageReplacementText_IsCaptured()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg")).ReplacementText = "Radzen";

        var figure = SingleFigure(document);

        Assert.False(figure.IsDecorative);
        Assert.Null(figure.AlternateText);
        Assert.Equal("Radzen", figure.ReplacementText);
        Assert.Empty(DocumentLayouter.Layout(document).Semantics.Structure.Artifacts);
    }

    [Fact]
    public void MissingInlineImageText_IsNotSilentlyClassifiedAsDecorative()
    {
        var document = new Document();
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        var laidOut = DocumentLayouter.Layout(document);
        var figure = Assert.Single(laidOut.Semantics.Structure.Nodes.Where(node => node.Intent == SemanticIntent.Figure));

        Assert.False(figure.IsDecorative);
        Assert.Empty(laidOut.Semantics.Structure.Artifacts);
    }

    [Fact]
    public void SectionWatermark_StampsInsideAnArtifactMarkedContentSequence()
    {
        var document = new Document();
        document.Language = "en-US";
        document.Info.Title = "Watermarked";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.Watermark = new Watermark { Text = "DRAFT" };
        section.Watermark.Font.Family = BuildTestSupport.Latin;
        section.Blocks.AddParagraph().Inlines.Add("Body").Font.Family = BuildTestSupport.Latin;

        var reader = BuildTestSupport.Read(document, Accessible());
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        var content = System.Text.Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));

        var begin = content.LastIndexOf("/Artifact BMC", System.StringComparison.Ordinal);
        Assert.True(begin >= 0, content);
        var end = content.IndexOf("EMC", begin, System.StringComparison.Ordinal);
        Assert.True(end > begin, content);
        Assert.Contains("Tj", content[begin..end], System.StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedTableHeaderRows_CarryTheirArtifactClassification()
    {
        var pages = DocumentLayouter.Layout(SpanningDocument(repeat: true, header: true)).Pages;

        Assert.True(pages.Length > 1);
        var continued = Assert.Single(pages[1].Body.Tables);
        Assert.Equal(SemanticArtifactKind.RepeatedContent, continued.Rows[0].Artifact);
        Assert.Null(continued.Rows[1].Artifact);
    }
}

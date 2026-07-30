#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Codes;
using Radzen.Documents.Geometry;

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

    private static string? Alt(DocumentReader reader, string type)
        => StructureTestHelpers.FindElement(reader, type) is { } element
            && element.TryGetValue("Alt", out var alt)
            && reader.Resolve(alt!) is StringObject text
            ? text.Value
            : null;

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

    private static List<string> MappedTypes(Document document, DocumentRenderer renderer)
    {
        var scene = DocumentLayouter.Layout(document);
        var tree = new StructureTreeBuilder(
            scene.Semantics,
            RenderRequest.From(renderer));
        tree.Build();
        var types = new List<string>();
        CollectTypes(tree.DocumentElement, types);
        return types;
    }

    [Fact]
    public void TableOfContents_CapturesNavigationIntentForEveryEntry()
    {
        var document = Chapters(out var toc);

        var structure = DocumentLayouter.Layout(document).Semantics.Structure;
        var nodes = structure.Nodes;
        var navigation = Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.Navigation));

        Assert.Equal(SemanticStructureTier.Structural, navigation.Tier);
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
    public void List_CapturesItsTierOnTheStructuralNode()
    {
        var document = new Document();
        var list = document.Sections.Add().Blocks.AddList(ListStyle.Bullet);
        list.AddItem("Item");

        var nodes = DocumentLayouter.Layout(document).Semantics.Structure.Nodes;
        var captured = Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.List));

        Assert.Equal(SemanticStructureTier.Structural, captured.Tier);
    }

    [Fact]
    public void TableOfContents_MapsToTheIsoNavigationStructureWhenTagged()
    {
        var document = Chapters(out var toc);

        var types = Types(BuildTestSupport.Read(document, Tagged()));

        Assert.Single(types.Where(type => type == "TOC"));
        Assert.Equal(toc.Entries.Count, types.Count(type => type == "TOCI"));
        Assert.Equal(toc.Entries.Count, types.Count(type => type == "Reference"));
        Assert.Equal(toc.Entries.Count, types.Count(type => type == "Link"));
    }

    [Fact]
    public void TableOfContents_RendersItsEntryTextUnderTheNavigationStructure()
    {
        var reader = BuildTestSupport.Read(Chapters(out _), Tagged());
        var root = TaggedStructureProbe.Root(reader);
        var navigation = TaggedStructureProbe.Single(root, "TOC");

        Assert.All(navigation.Children, entry =>
        {
            var reference = Assert.Single(entry.Children);
            var link = Assert.Single(reference.Children);
            Assert.Equal("Link", link.Type);
            Assert.NotEmpty(link.Mcids);
            Assert.NotEmpty(link.ObjectReferences);
        });
    }

    [Fact]
    public void TableOfContents_MapsToNothingWhenUntagged()
    {
        var types = MappedTypes(Chapters(out _), new DocumentRenderer());

        Assert.DoesNotContain("TOC", types);
        Assert.DoesNotContain("TOCI", types);
        Assert.DoesNotContain("Reference", types);
    }

    [Fact]
    public void TableOfContents_InUntaggedOutput_AddsNoStructure()
    {
        var document = Chapters(out _);

        var types = Types(BuildTestSupport.Read(document, new DocumentRenderer()));

        Assert.DoesNotContain("TOC", types);
        Assert.DoesNotContain("TOCI", types);
    }

    [Fact]
    public void QrCodeWithAlternateText_IsAMeaningfulFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80)).AlternateText = "Scan for details";

        var reader = BuildTestSupport.Read(document, Accessible());

        Assert.Contains("Figure", Types(reader));
        Assert.Equal("Scan for details", Alt(reader, "Figure"));
    }

    [Fact]
    public void QrCodeWithoutAlternateText_StaysDecorative()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80));

        Assert.DoesNotContain("Figure", Types(BuildTestSupport.Read(document, Accessible())));
    }

    private static SemanticStructureNode SingleFigure(Document document)
    {
        var nodes = DocumentLayouter.Layout(document).Semantics.Structure.Nodes;
        return Assert.Single(nodes.Where(node => node.Intent == SemanticIntent.Figure));
    }

    [Fact]
    public void QrCodeWithoutAlternateText_IsStillCapturedAsADecorativeFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80));

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
    public void BarcodeWithoutAlternateText_IsStillCapturedAsADecorativeFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(160), Unit.FromPoint(40));

        Assert.True(SingleFigure(document).IsDecorative);
    }

    [Fact]
    public void InlineImageWithoutAlternateText_IsStillCapturedAsADecorativeFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Inline";
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Logo: ").Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        Assert.True(SingleFigure(document).IsDecorative);
    }

    [Fact]
    public void DecorativeQrCode_IsDrawnAsArtifactContent()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddQrCode("RADZEN", Unit.FromPoint(80));

        var reader = BuildTestSupport.Read(document, Accessible());

        Assert.All(FillTags(reader), tag => Assert.Equal("Artifact", tag));
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
    public void BarcodeWithAlternateText_IsAMeaningfulFigure()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        var barcode = document.Sections.Add().Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(160), Unit.FromPoint(40));
        barcode.AlternateText = "Order RADZEN";

        var reader = BuildTestSupport.Read(document, Accessible());

        Assert.Contains("Figure", Types(reader));
        Assert.Equal("Order RADZEN", Alt(reader, "Figure"));
    }

    [Fact]
    public void BarcodeWithoutAlternateText_StaysDecorative()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Code";
        BuildTestSupport.RegisterLatin(document);
        document.Sections.Add().Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN", Unit.FromPoint(160), Unit.FromPoint(40));

        Assert.DoesNotContain("Figure", Types(BuildTestSupport.Read(document, Accessible())));
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

        var reader = BuildTestSupport.Read(document, Accessible());

        Assert.Contains("Figure", Types(reader));
        Assert.Equal("The Radzen logo", Alt(reader, "Figure"));
    }

    [Fact]
    public void InlineImageWithoutAlternateText_StaysDecorative()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Inline";
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Inlines.Add("Logo: ").Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        Assert.DoesNotContain("Figure", Types(BuildTestSupport.Read(document, Accessible())));
    }

    [Fact]
    public void Container_GroupsItsBlocksUnderADivWhenTagged()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Boxed";
        BuildTestSupport.RegisterLatin(document);
        var container = document.Sections.Add().Blocks.Add(new Container { Padding = Unit.FromPoint(8) });
        container.Blocks.AddParagraph().Inlines.Add("BOXED").Font.Family = BuildTestSupport.Latin;

        var root = TaggedStructureProbe.Root(BuildTestSupport.Read(document, Accessible()));
        var div = TaggedStructureProbe.Single(root, "Div");

        Assert.Equal("P", Assert.Single(div.Children).Type);
    }

    [Fact]
    public void Container_AddsNoGroupingToUntaggedOutput()
    {
        var document = new Document();
        document.Info.Title = "Boxed";
        BuildTestSupport.RegisterLatin(document);
        var container = document.Sections.Add().Blocks.Add(new Container { Padding = Unit.FromPoint(8) });
        container.Blocks.AddParagraph().Inlines.Add("BOXED").Font.Family = BuildTestSupport.Latin;

        Assert.DoesNotContain("Div", Types(BuildTestSupport.Read(document, new DocumentRenderer())));
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
        var types = Types(BuildTestSupport.Read(SpanningDocument(repeat: true, header: false), Accessible()));

        Assert.DoesNotContain("TH", types);
        Assert.Contains("TD", types);
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
        Assert.Contains("TH", Types(BuildTestSupport.Read(document, Accessible())));
    }
}

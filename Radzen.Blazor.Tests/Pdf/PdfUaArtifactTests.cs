#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Codes;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfUaArtifactTests
{
    private static DocumentReader ReadAuthored((Document Document, DocumentRenderer Renderer) authored)
        => BuildTestSupport.Read(authored.Document, authored.Renderer);

    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) AuthorBanded(bool ua, PdfAConformance conformance = PdfAConformance.None)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Banded";
        var builderRenderer = new DocumentRenderer();
        builderRenderer.Accessibility = ua ? PdfUaConformance.PdfUa1 : PdfUaConformance.None;
        builderRenderer.Conformance = conformance;
        if (ua)
        {
            document.Language = "en-US";
        }

        var section = document.Sections.Add();

        var header = new Paragraph();
        header.Inlines.Add("HEADER").Font.Family = BuildTestSupport.Latin;
        section.Header.Blocks.Add(header);

        var footer = new Paragraph();
        footer.Inlines.Add("FOOTER").Font.Family = BuildTestSupport.Latin;
        section.Footer.Blocks.Add(footer);

        BuildTestSupport.AddText(section, "Body paragraph", BuildTestSupport.Latin);

        var table = section.Blocks.Add(new Table());
        table.Columns.Add();
        table.Columns.Add();
        table.Borders.SetAll(width: 1, color: Color.FromRgb(0, 0, 0));

        var row = table.Rows.Add();
        var left = TableLayoutSupport.Fill(row.Cells[0], "Item");
        left.Background = Color.FromRgb(220, 220, 220);
        TableLayoutSupport.Fill(row.Cells[1], "Price");

        return (document, builderRenderer);
    }

    private static byte[] ContentOf((Document Document, DocumentRenderer Renderer) authored)
    {
        var reader = BuildTestSupport.Read(authored.Document, authored.Renderer);
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        return BuildTestSupport.Content(reader, page);
    }

    private static readonly HashSet<string> PaintingOps = new(StringComparer.Ordinal)
    {
        "Tj", "TJ", "'", "\"", "Do", "f", "F", "f*", "S", "s", "B", "B*", "b", "b*", "sh",
    };

    private static List<string> PaintingOpsOutsideMarkedContent(DocumentReader reader, DictionaryObject page)
    {
        var offenders = new List<string>();
        var depth = 0;
        foreach (var operation in ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page)))
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    depth++;
                    break;
                case "EMC":
                    depth--;
                    break;
                default:
                    if (depth == 0 && PaintingOps.Contains(operation.Operator))
                    {
                        offenders.Add(operation.Operator);
                    }

                    break;
            }
        }

        return offenders;
    }

    private static void AssertMarkedContentBalanced(DocumentReader reader, DictionaryObject page)
    {
        var depth = 0;
        foreach (var operation in ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page)))
        {
            if (operation.Operator is "BDC" or "BMC")
            {
                depth++;
            }
            else if (operation.Operator == "EMC")
            {
                depth--;
                Assert.True(depth >= 0, "EMC without a matching BDC/BMC");
            }
        }

        Assert.Equal(0, depth);
    }

    [Fact]
    public void TaggedDocument_LeavesNoRealContentOutsideMarkedContent()
    {
        var reader = ReadAuthored(AuthorBanded(ua: true));
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;

        AssertMarkedContentBalanced(reader, page);
        Assert.Empty(PaintingOpsOutsideMarkedContent(reader, page));
    }

    [Fact]
    public void TaggedDocument_HeaderContentIsInsideArtifact()
    {
        var tags = ContentOperationTestHelpers.Tags(ContentOf(AuthorBanded(ua: true)));

        Assert.Contains(
            tags,
            tag => tag.Tag == "Artifact"
                && tag.Properties.TryGetValue("Type", out var type)
                && type == "Pagination");
    }

    [Fact]
    public void LevelA_TaggedDocument_LeavesNoRealContentOutsideMarkedContent()
    {
        var reader = ReadAuthored(AuthorBanded(ua: false, conformance: PdfAConformance.PdfA3A));
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;

        AssertMarkedContentBalanced(reader, page);
        Assert.Empty(PaintingOpsOutsideMarkedContent(reader, page));
    }

    [Fact]
    public void PlainDocument_IsNotWrappedInArtifact()
    {
        var tags = ContentOperationTestHelpers.Tags(ContentOf(AuthorBanded(ua: false)));

        Assert.DoesNotContain(tags, tag => tag.Tag == "Artifact");
    }

    [Fact]
    public void PlainDocument_IsByteIdenticalAcrossBuilds()
    {
        Assert.Equal(RenderAuthored(AuthorBanded(ua: false)), RenderAuthored(AuthorBanded(ua: false)));
    }

    [Fact]
    public void PdfUA_WithoutTitle_Throws()
    {
        var (document, builderRenderer) = AuthorBanded(ua: true);
        document.Info.Title = null;

        var error = Assert.Throws<InvalidOperationException>(() => builderRenderer.ToArray(document));

        Assert.Contains("PDF/UA", error.Message, StringComparison.Ordinal);
        Assert.Contains("Document.Info.Title", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_DecorativeImage_IsNotTagged()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Figure";
        var section = document.Sections.Add();
        section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg"))).AlternateText = "";

        Lacks("emission", "/S /Figure", Emit(document, builderRenderer));
    }

    [Theory]
    [InlineData("Image", false)]
    [InlineData("InlineImage", false)]
    [InlineData("Barcode", false)]
    [InlineData("QrCode", false)]
    [InlineData("Image", true)]
    [InlineData("InlineImage", true)]
    [InlineData("Barcode", true)]
    [InlineData("QrCode", true)]
    public void AccessibleFigureThatDescribesNothing_Throws(string kind, bool levelA)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Figure";
        var blocks = document.Sections.Add().Blocks;
        switch (kind)
        {
            case "Image":
                blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
                break;
            case "InlineImage":
                blocks.Add(new Paragraph()).Inlines.Add(new InlineImage(PdfTestResources.Open("Images/rgb.jpg")));
                break;
            case "Barcode":
                blocks.Add(new Barcode(BarcodeType.Code128, "RADZEN", Unit.FromPoint(160), Unit.FromPoint(40)));
                break;
            case "QrCode":
                blocks.Add(new QrCode("RADZEN", Unit.FromPoint(80)));
                break;
        }

        var renderer = levelA
            ? new DocumentRenderer { Conformance = PdfAConformance.PdfA3A }
            : new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };

        var error = Assert.Throws<InvalidOperationException>(() => renderer.ToArray(document));

        Assert.Contains(levelA ? "PDF/A" : "PDF/UA", error.Message, StringComparison.Ordinal);
        Assert.Contains("AlternateText", error.Message, StringComparison.Ordinal);
        Assert.Contains("decorative", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_EmptyAlternateText_DeclaresTheImageDecorativeAndIsNotTagged()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Figure";
        var image = document.Sections.Add().Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.AlternateText = "";

        var emission = Emit(document, new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        Lacks("emission", "/S /Figure", emission);
    }

    [Fact]
    public void PdfUA_ParagraphStyledAsFigureWithoutAlternateText_Throws()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Figure";
        document.Styles.Add("Fig").Role = "Figure";
        var paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.StyleName = "Fig";
        paragraph.Inlines.Add("Stands in for a picture.").Font.Family = BuildTestSupport.Latin;

        var error = Assert.Throws<InvalidOperationException>(() => builderRenderer.ToArray(document));

        Assert.Contains("PDF/UA", error.Message, StringComparison.Ordinal);
        Assert.Contains("Figure", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_FigureWithReplacementText_DoesNotThrow()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Figure";
        var section = document.Sections.Add();
        var image = section.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));
        image.ReplacementText = "chart";

        Assert.Null(Record.Exception(() => builderRenderer.ToArray(document)));
    }

    [Fact]
    public void LevelA_WithList_UsesCapturedStructuralTags()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3A };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "List";
        var section = document.Sections.Add();
        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Bullet });
        list.Font.Family = BuildTestSupport.Latin;
        list.Items.Add("First");

        var emission = Emit(document, builderRenderer);

        Shaped("structure tree", @"/S /L[^A-Za-z]", emission);
        Shaped("structure tree", @"/S /LI[^A-Za-z]", emission);
        Shaped("structure tree", @"/S /Lbl[^A-Za-z]", emission);
        Shaped("structure tree", @"/S /LBody[^A-Za-z]", emission);
    }

    [Fact]
    public void PdfUA_WithList_DoesNotThrow()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "List";
        var section = document.Sections.Add();
        var list = section.Blocks.Add(new ListBlock { Style = ListStyle.Bullet });
        list.Font.Family = BuildTestSupport.Latin;
        list.Items.Add("First");

        Assert.Null(Record.Exception(() => builderRenderer.ToArray(document)));
    }

    [Fact]
    public void PdfUA_WithLinkInsideTheBody_IsAccepted()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Link";
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Radzen");
        run.Font.Family = BuildTestSupport.Latin;
        run.Link = "https://www.radzen.com";
        section.Blocks.Add(paragraph);

        Assert.Null(Record.Exception(() => builderRenderer.ToArray(document)));
    }

    [Fact]
    public void PdfUA_WithLinkInThePageHeaderArtifact_Throws()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Link";
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Radzen");
        run.Font.Family = BuildTestSupport.Latin;
        run.Link = "https://www.radzen.com";
        section.Header.Blocks.Add(paragraph);
        section.Blocks.Add(new Paragraph()).Inlines.Add("Body").Font.Family = BuildTestSupport.Latin;

        var error = Assert.Throws<InvalidOperationException>(() => builderRenderer.ToArray(document));

        Assert.Contains("PDF/UA", error.Message, StringComparison.Ordinal);
    }
}

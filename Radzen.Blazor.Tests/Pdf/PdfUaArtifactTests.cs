#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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

        var table = section.Blocks.AddTable();
        table.Columns.Add();
        table.Columns.Add();
        table.Borders.Width = 1;
        table.Borders.Color = Color.FromRgb(0, 0, 0);

        var row = table.Rows.Add();
        var left = TableLayoutSupport.Fill(row.Cells[0], "Item");
        left.Background = Color.FromRgb(220, 220, 220);
        TableLayoutSupport.Fill(row.Cells[1], "Price");

        return (document, builderRenderer);
    }

    private static string ContentStringOf((Document Document, DocumentRenderer Renderer) authored)
    {
        var reader = BuildTestSupport.Read(authored.Document, authored.Renderer);
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, page));
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
        var content = ContentStringOf(AuthorBanded(ua: true));
        Assert.Contains("/Artifact <</Type /Pagination>> BDC", content, StringComparison.Ordinal);
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
        var content = ContentStringOf(AuthorBanded(ua: false));
        Assert.DoesNotContain("/Artifact", content, StringComparison.Ordinal);
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

        var exception = Record.Exception(() => builderRenderer.ToArray(document));
        Assert.NotNull(exception);
        Assert.Contains("title", exception!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PdfUA_FigureWithoutAltOrActualText_Throws()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Figure";
        var section = document.Sections.Add();
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        var exception = Record.Exception(() => builderRenderer.ToArray(document));
        Assert.NotNull(exception);
        Assert.Contains("Figure", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_FigureWithActualText_DoesNotThrow()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Figure";
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.ActualText = "chart";

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
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Family = BuildTestSupport.Latin;
        list.AddItem("First");

        var reader = BuildTestSupport.Read(document, builderRenderer);
        var types = new List<string>();
        StructureTestHelpers.CollectTypes(reader, StructureTestHelpers.RootKids(reader), types);

        Assert.Contains("L", types);
        Assert.Contains("LI", types);
        Assert.Contains("Lbl", types);
        Assert.Contains("LBody", types);
    }

    [Fact]
    public void PdfUA_WithList_DoesNotThrow()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "List";
        var section = document.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Family = BuildTestSupport.Latin;
        list.AddItem("First");

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
        section.Blocks.AddParagraph().Inlines.Add("Body").Font.Family = BuildTestSupport.Latin;

        var exception = Record.Exception(() => builderRenderer.ToArray(document));
        Assert.NotNull(exception);
        Assert.Contains("link", exception!.Message, StringComparison.OrdinalIgnoreCase);
    }
}

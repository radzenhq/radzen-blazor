#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// PDF/UA and PDF/A Level-A artifact wiring and fail-loud conformance. Tagged output must
// carry ZERO untagged real content: pagination (header/footer) and decorative
// (background/border/watermark) content is wrapped in /Artifact BDC..EMC, while a plain
// untagged document is left byte-identical (no /Artifact markers). ConformanceWriter must
// throw rather than silently advertise a conformance it cannot meet.
public class PdfUaArtifactTests
{
    private static DocumentBuilder AuthorBanded(bool ua, PdfAConformance conformance = PdfAConformance.None)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        builder.Info.Title = "Banded";
        builder.PdfUA = ua;
        builder.Conformance = conformance;
        if (ua)
        {
            builder.Language = "en-US";
        }

        var section = builder.Sections.Add();

        var header = new Paragraph();
        header.Inlines.Add("HEADER").Font.Name = BuildTestSupport.Latin;
        section.Header.Blocks.Add(header);

        var footer = new Paragraph();
        footer.Inlines.Add("FOOTER").Font.Name = BuildTestSupport.Latin;
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

        return builder;
    }

    private static string ContentString(DocumentBuilder builder)
    {
        var reader = BuildTestSupport.Read(builder);
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, page));
    }

    private static readonly HashSet<string> PaintingOps = new(StringComparer.Ordinal)
    {
        "Tj", "TJ", "'", "\"", "Do", "f", "F", "f*", "S", "s", "B", "B*", "b", "b*", "sh",
    };

    // Walks the content stream tracking marked-content nesting; returns the painting
    // operators that appear at marked-content depth 0 (neither tagged nor artifact).
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
        var reader = BuildTestSupport.Read(AuthorBanded(ua: true));
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;

        AssertMarkedContentBalanced(reader, page);
        Assert.Empty(PaintingOpsOutsideMarkedContent(reader, page));
    }

    [Fact]
    public void TaggedDocument_HeaderContentIsInsideArtifact()
    {
        var content = ContentString(AuthorBanded(ua: true));
        Assert.Contains("/Artifact BDC", content, StringComparison.Ordinal);
    }

    [Fact]
    public void LevelA_TaggedDocument_LeavesNoRealContentOutsideMarkedContent()
    {
        var reader = BuildTestSupport.Read(AuthorBanded(ua: false, conformance: PdfAConformance.PdfA3A));
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;

        AssertMarkedContentBalanced(reader, page);
        Assert.Empty(PaintingOpsOutsideMarkedContent(reader, page));
    }

    [Fact]
    public void PlainDocument_IsNotWrappedInArtifact()
    {
        var content = ContentString(AuthorBanded(ua: false));
        Assert.DoesNotContain("/Artifact", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainDocument_IsByteIdenticalAcrossBuilds()
    {
        Assert.Equal(AuthorBanded(ua: false).ToArray(), AuthorBanded(ua: false).ToArray());
    }

    [Fact]
    public void PdfUA_WithoutTitle_Throws()
    {
        var builder = AuthorBanded(ua: true);
        builder.Info.Title = null;

        var exception = Record.Exception(() => builder.ToArray());
        Assert.NotNull(exception);
        Assert.Contains("title", exception!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PdfUA_FigureWithoutAltOrActualText_Throws()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        builder.Info.Title = "Figure";
        var section = builder.Sections.Add();
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        var exception = Record.Exception(() => builder.ToArray());
        Assert.NotNull(exception);
        Assert.Contains("Figure", exception!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUA_FigureWithActualText_DoesNotThrow()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        builder.Info.Title = "Figure";
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.ActualText = "chart";

        Assert.Null(Record.Exception(() => builder.ToArray()));
    }

    [Fact]
    public void LevelA_WithUntaggedList_Throws()
    {
        var builder = new DocumentBuilder { Conformance = PdfAConformance.PdfA3A };
        BuildTestSupport.RegisterLatin(builder);
        builder.Info.Title = "List";
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = BuildTestSupport.Latin;
        list.AddItem("First");

        var exception = Record.Exception(() => builder.ToArray());
        Assert.NotNull(exception);
        Assert.Contains("list", exception!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PdfUA_WithList_DoesNotThrow()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        BuildTestSupport.RegisterLatin(builder);
        builder.Info.Title = "List";
        var section = builder.Sections.Add();
        var list = section.Blocks.AddList(ListStyle.Bullet);
        list.Font.Name = BuildTestSupport.Latin;
        list.AddItem("First");

        Assert.Null(Record.Exception(() => builder.ToArray()));
    }

    [Fact]
    public void PdfUA_WithLinkAnnotation_Throws()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        BuildTestSupport.RegisterLatin(builder);
        builder.Info.Title = "Link";
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Radzen");
        run.Font.Name = BuildTestSupport.Latin;
        run.Link = "https://www.radzen.com";
        section.Blocks.Add(paragraph);

        var exception = Record.Exception(() => builder.ToArray());
        Assert.NotNull(exception);
        Assert.Contains("link", exception!.Message, StringComparison.OrdinalIgnoreCase);
    }
}

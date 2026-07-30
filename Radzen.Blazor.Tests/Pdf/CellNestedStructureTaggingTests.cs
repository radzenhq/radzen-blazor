#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class CellNestedStructureTaggingTests
{
    private static (Document Document, DocumentRenderer Renderer) AuthorTableWithCellImage(bool alternateText)
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Cell image";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var image = row.Cells[0].Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        if (alternateText)
        {
            image.AlternateText = "A red square";
        }

        return (document, builderRenderer);
    }

    private static List<ContentOperation> Ops((Document Document, DocumentRenderer Renderer) authored)
        => ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(
            BuildTestSupport.Read(authored.Document, authored.Renderer), 0));

    private static HashSet<string> TagsWrappingText(List<ContentOperation> ops)
    {
        var stack = new List<string>();
        var tags = new HashSet<string>();
        foreach (var operation in ops)
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    stack.Add(operation.Operands.Count > 0 ? operation.Operands[0].Text : "");
                    break;
                case "EMC":
                    if (stack.Count > 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }

                    break;
                case "Tj" or "TJ" or "'" or "\"":
                    foreach (var tag in stack)
                    {
                        tags.Add(tag);
                    }

                    break;
            }
        }

        return tags;
    }

    private static HashSet<string> TagsWrappingImages(List<ContentOperation> ops)
    {
        var stack = new List<string>();
        var tags = new HashSet<string>();
        foreach (var operation in ops)
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    stack.Add(operation.Operands.Count > 0 ? operation.Operands[0].Text : "");
                    break;
                case "EMC":
                    if (stack.Count > 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }

                    break;
                case "Do":
                    foreach (var tag in stack)
                    {
                        tags.Add(tag);
                    }

                    break;
            }
        }

        return tags;
    }

    [Fact]
    public void PdfUA_CellImageWithoutAltText_IsRejected()
    {
        var (document, builderRenderer) = AuthorTableWithCellImage(alternateText: false);
        var ex = Assert.Throws<InvalidOperationException>(() => builderRenderer.ToArray(document));
        Assert.Contains("Figure", ex.Message);
    }

    [Fact]
    public void PdfUA_CellImageWithAltText_TaggedAsFigure()
    {
        Assert.Contains("Figure", TagsWrappingImages(Ops(AuthorTableWithCellImage(alternateText: true))));
    }

    [Fact]
    public void PdfUA_CellListContent_IsTagged()
    {
        var document = new Document { Language = "en-US" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Cell list";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var list = row.Cells[0].Blocks.AddList();
        var item = list.AddItem();
        item.Font.Family = BuildTestSupport.Latin;
        item.Inlines.Add("One").Font.Family = BuildTestSupport.Latin;

        Assert.Contains("P", TagsWrappingText(Ops((document, builderRenderer))));
    }
}

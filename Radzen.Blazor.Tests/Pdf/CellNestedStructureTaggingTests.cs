#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class CellNestedStructureTaggingTests
{
    private static DocumentBuilder AuthorTableWithCellImage(bool alternateText)
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        builder.Info.Title = "Cell image";
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var image = row.Cells[0].Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        if (alternateText)
        {
            image.AlternateText = "A red square";
        }

        return builder;
    }

    private static List<ContentOperation> Ops(DocumentBuilder builder)
        => ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(BuildTestSupport.Read(builder), 0));

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
        var builder = AuthorTableWithCellImage(alternateText: false);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.ToArray());
        Assert.Contains("Figure", ex.Message);
    }

    [Fact]
    public void PdfUA_CellImageWithAltText_TaggedAsFigure()
    {
        var builder = AuthorTableWithCellImage(alternateText: true);
        Assert.Contains("Figure", TagsWrappingImages(Ops(builder)));
    }

    [Fact]
    public void PdfUA_CellListContent_IsTagged()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en-US" };
        builder.Info.Title = "Cell list";
        BuildTestSupport.RegisterLatin(builder);

        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        var list = row.Cells[0].Blocks.AddList();
        var item = list.AddItem();
        item.Font.Name = BuildTestSupport.Latin;
        item.Inlines.Add("One").Font.Name = BuildTestSupport.Latin;

        Assert.Contains("LBody", TagsWrappingText(Ops(builder)));
    }
}

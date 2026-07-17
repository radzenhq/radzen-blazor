#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentElementTests
{
    private static int IndexOfOperator(byte[] content, string op)
        => ContentStreamTokenizer.Operators(content).IndexOf(op);

    private static ContentOperation? FindOperator(byte[] content, string op)
    {
        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == op)
            {
                return operation;
            }
        }

        return null;
    }

    [Fact]
    public void Content_IsEmptyByDefault()
    {
        var document = new Document();
        var page = document.Pages.Add();

        Assert.Empty(page.Content);
        Assert.Equal(0, page.Content.Count);
    }

    [Fact]
    public void Content_IsReadOnlyListOfContentElement()
    {
        var document = new Document();
        var page = document.Pages.Add();

        Assert.IsAssignableFrom<IReadOnlyList<ContentElement>>(page.Content);
    }

    [Fact]
    public void Add_ReturnsSameInstanceAndAppends()
    {
        var document = new Document();
        var page = document.Pages.Add();

        var text = page.Content.Add(new TextContent("Hi", 10, 20));

        Assert.Single(page.Content);
        Assert.Same(text, page.Content[0]);
    }

    [Fact]
    public void Add_PreservesInsertionOrderInCollection()
    {
        var document = new Document();
        var page = document.Pages.Add();

        var a = page.Content.Add(new TextContent("A", 0, 0));
        var b = page.Content.Add(new PathContent());
        var c = page.Content.Add(new TextContent("C", 0, 0));

        Assert.Equal(3, page.Content.Count);
        Assert.Same(a, page.Content[0]);
        Assert.Same(b, page.Content[1]);
        Assert.Same(c, page.Content[2]);
    }

    [Fact]
    public void ContentElement_HasIdentityTransformByDefault()
    {
        var element = new TextContent("x", 0, 0);

        Assert.Equal(Matrix.Identity, element.Transform);
    }

    [Fact]
    public void ContentElement_IsNotArtifactByDefault()
    {
        var element = new TextContent("x", 0, 0);

        Assert.False(element.IsArtifact);
    }

    [Fact]
    public void NonEmptyContent_EmitsContentStream()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hello", 72, 700));

        var reader = ContentTestHelpers.Reload(document);
        var content = ContentTestHelpers.PageContent(reader, 0);

        Assert.Contains("BT", ContentStreamTokenizer.Operators(content));
        Assert.Contains("ET", ContentStreamTokenizer.Operators(content));
    }

    [Fact]
    public void ZOrder_TextBeforePath_MatchesAddOrder()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Z", 10, 10));
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(50, 50);

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        var textIndex = IndexOfOperator(content, "Tj");
        var pathIndex = IndexOfOperator(content, "m");
        Assert.True(textIndex >= 0);
        Assert.True(pathIndex >= 0);
        Assert.True(textIndex < pathIndex);
    }

    [Fact]
    public void ZOrder_PathBeforeText_MatchesAddOrder()
    {
        var document = new Document();
        var page = document.Pages.Add();
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(50, 50);
        page.Content.Add(new TextContent("Z", 10, 10));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        var textIndex = IndexOfOperator(content, "Tj");
        var pathIndex = IndexOfOperator(content, "m");
        Assert.True(pathIndex < textIndex);
    }

    [Fact]
    public void Transform_NonIdentity_EmittedAsCm()
    {
        var document = new Document();
        var page = document.Pages.Add();
        var matrix = Matrix.Scale(2, 3) * Matrix.Translate(10, 20);
        page.Content.Add(new TextContent("T", 0, 0) { Transform = matrix });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var cm = FindOperator(content, "cm");

        Assert.NotNull(cm);
        Assert.Equal(6, cm!.Operands.Count);
        Assert.Equal(matrix.A, cm.Num(0), 3);
        Assert.Equal(matrix.B, cm.Num(1), 3);
        Assert.Equal(matrix.C, cm.Num(2), 3);
        Assert.Equal(matrix.D, cm.Num(3), 3);
        Assert.Equal(matrix.E, cm.Num(4), 3);
        Assert.Equal(matrix.F, cm.Num(5), 3);
    }

    [Fact]
    public void Transform_Identity_EmitsNoCm()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("T", 0, 0));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.DoesNotContain("cm", ContentStreamTokenizer.Operators(content));
    }

    [Fact]
    public void IsArtifact_WrapsElementInArtifactMarkedContent()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A", 0, 0) { IsArtifact = true });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        Assert.Contains("BMC", operators);
        Assert.Contains("EMC", operators);

        var bmc = FindOperator(content, "BMC");
        Assert.NotNull(bmc);
        Assert.Single(bmc!.Operands);
        Assert.Equal(ContentTokenKind.Name, bmc.Operands[0].Kind);
        Assert.Equal("Artifact", bmc.Operands[0].Text);

        var bmcIndex = operators.IndexOf("BMC");
        var emcIndex = operators.IndexOf("EMC");
        var tjIndex = operators.IndexOf("Tj");
        Assert.True(bmcIndex < tjIndex);
        Assert.True(tjIndex < emcIndex);
    }

    [Fact]
    public void DefaultElement_NotWrappedInArtifact()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("A", 0, 0));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.DoesNotContain("BDC", ContentStreamTokenizer.Operators(content));
    }

    [Fact]
    public void MultipleElements_OperatorOrderMatchesElementOrder()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("first", 0, 0));
        var path = page.Content.Add(new PathContent());
        path.MoveTo(0, 0);
        path.LineTo(1, 1);
        page.Content.Add(new TextContent("third", 0, 0));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        var firstBt = operators.IndexOf("BT");
        var m = operators.IndexOf("m");
        var lastEt = operators.LastIndexOf("ET");
        Assert.True(firstBt < m);
        Assert.True(m < lastEt);
    }

    [Fact]
    public void NonEmptyContent_OverridesRawSetContent()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("q Q"));
        page.Content.Add(new TextContent("MARKER", 10, 10));

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var text = Encoding.ASCII.GetString(content);

        Assert.Contains("MARKER", text);
    }

    [Fact]
    public void EmptyContent_FallsBackToRawSetContent()
    {
        var raw = Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm");
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(raw);

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.Contains("cm", ContentStreamTokenizer.Operators(content));
    }

    [Fact]
    public void ImageContent_ExposesRect()
    {
        var image = new ImageContent([1, 2, 3, 4]) { Bounds = PdfRect.FromSize(10, 20, 100, 50) };

        Assert.Equal(PdfRect.FromSize(10, 20, 100, 50), image.Bounds);
        Assert.IsAssignableFrom<ContentElement>(image);
    }

    [Fact]
    public void ImageContent_UndecodablePayload_ThrowsOnSave()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent([0, 0, 0, 0]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) });

        Assert.Throws<NotSupportedException>(() => document.ToArray());
    }

}

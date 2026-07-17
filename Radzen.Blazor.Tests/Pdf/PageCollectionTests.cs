using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class PageCollectionTests
{
    [Fact]
    public void NewDocument_HasEmptyPageCollection()
    {
        var document = new Document();

        Assert.Empty(document.Pages);
        Assert.Equal(0, document.Pages.Count);
    }

    [Fact]
    public void Add_Default_IsA4Portrait()
    {
        var document = new Document();
        var page = document.Pages.Add();

        Assert.Equal(PageSizes.A4.Width, page.Width);
        Assert.Equal(PageSizes.A4.Height, page.Height);
    }

    [Fact]
    public void Add_WithSize_SetsWidthAndHeight()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.Letter);

        Assert.Equal(PageSizes.Letter.Width, page.Width);
        Assert.Equal(PageSizes.Letter.Height, page.Height);
    }

    [Fact]
    public void Add_Portrait_KeepsWidthAndHeight()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.A5, PageOrientation.Portrait);

        Assert.Equal(PageSizes.A5.Width, page.Width);
        Assert.Equal(PageSizes.A5.Height, page.Height);
    }

    [Fact]
    public void Add_Landscape_SwapsWidthAndHeight()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.A5, PageOrientation.Landscape);

        Assert.Equal(PageSizes.A5.Height, page.Width);
        Assert.Equal(PageSizes.A5.Width, page.Height);
        Assert.True(page.Width > page.Height);
    }

    [Fact]
    public void Add_ReturnsSamePageAsIndexer()
    {
        var document = new Document();
        var page = document.Pages.Add(PageSizes.Letter);

        Assert.Same(page, document.Pages[0]);
    }

    [Fact]
    public void Pages_CountAndIndexerReflectAddOrder()
    {
        var document = new Document();
        var first = document.Pages.Add(PageSizes.A4);
        var second = document.Pages.Add(PageSizes.Letter);
        var third = document.Pages.Add(PageSizes.A5, PageOrientation.Landscape);

        Assert.Equal(3, document.Pages.Count);
        Assert.Same(first, document.Pages[0]);
        Assert.Same(second, document.Pages[1]);
        Assert.Same(third, document.Pages[2]);
    }

    [Fact]
    public void Pages_IsEnumerableInOrder()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);
        document.Pages.Add(PageSizes.Letter);

        var widths = new List<Unit>();
        foreach (var page in document.Pages)
        {
            widths.Add(page.Width);
        }

        Assert.Equal(new[] { PageSizes.A4.Width, PageSizes.Letter.Width }, widths);
    }

    [Fact]
    public void Pages_IsReadOnlyList()
    {
        var document = new Document();

        Assert.IsAssignableFrom<IReadOnlyList<Page>>(document.Pages);
    }

    [Fact]
    public void SetContent_RoundTripsThroughGetContent()
    {
        var content = Encoding.ASCII.GetBytes("q 1 0 0 1 0 0 cm Q");
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(content);

        Assert.Equal(content, page.GetContent());
    }

    [Fact]
    public void GetContent_NullWhenNoContentSet()
    {
        var document = new Document();
        var page = document.Pages.Add();

        Assert.Null(page.GetContent());
    }

    [Fact]
    public void RemoveAt_DropsPageAndShiftsIndices()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);
        var second = document.Pages.Add(PageSizes.Letter);

        document.Pages.RemoveAt(0);

        Assert.Equal(1, document.Pages.Count);
        Assert.Same(second, document.Pages[0]);
    }

    [Fact]
    public void Insert_PlacesPageAtIndex()
    {
        var document = new Document();
        var first = document.Pages.Add(PageSizes.A4);
        var second = document.Pages.Add(PageSizes.Letter);

        document.Pages.RemoveAt(0);
        document.Pages.Insert(1, first);

        Assert.Equal(2, document.Pages.Count);
        Assert.Same(second, document.Pages[0]);
        Assert.Same(first, document.Pages[1]);
    }
}

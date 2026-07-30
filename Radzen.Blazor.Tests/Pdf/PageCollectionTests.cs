using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class PageCollectionTests
{
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
}

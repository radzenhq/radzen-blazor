#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageContentChangeTrackingTests
{
    [Fact]
    public void MutatingBounds_MarksTheElementModified()
    {
        var image = new ImageContent([1, 2, 3, 4]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) };
        image.AcceptChanges();

        image.Bounds = PdfRect.FromSize(5, 5, 20, 20);

        Assert.True(image.IsModified);
    }

    [Fact]
    public void ImageContent_IsNeverMaterializedFromALoadedPage()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent(PdfTestResources.ReadAllBytes("Images/rgb.png"))
        {
            Bounds = PdfRect.FromSize(10, 10, 40, 20),
        });

        var reloaded = InterpreterTestSupport.Load(document.ToArray()).Pages[0].Content;

        Assert.NotEmpty(reloaded);
        Assert.Empty(reloaded.OfType<ImageContent>());
        Assert.NotEmpty(reloaded.OfType<XObjectContent>());
    }
}

#nullable enable
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ImageContent.Bounds reaches the emitted cm operands, so it must open a change-detection
// door like every other mutable member. ContentElementChangeTrackingMatrixTests cannot
// cover it: the matrix only walks kinds the interpreter can materialize.
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

    // Pins why the above is a convention fix rather than a live bug: IsModified is only
    // consulted over a loaded page's materialized prefix, and an image XObject comes back
    // as XObjectContent, so an ImageContent can never occupy that prefix.
    [Fact]
    public void ImageContent_IsNeverMaterializedFromALoadedPage()
    {
        var document = new Document();
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

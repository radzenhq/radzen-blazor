#nullable enable
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class RectConventionTests
{
    private const double PageHeight = 792;

    [Fact]
    public void AnnotationRect_EmitsLowerLeftThenUpperRight()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.Letter);
        page.Annotations.Add(new SquareAnnotation(PdfRect.FromSize(10, 20, 100, 50)));

        Assert.Equal([10, 20, 110, 70], SavedNumbers(document, "Rect"));
    }

    [Fact]
    public void MarkupQuadPoints_PutTheTopEdgeFirst()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.Letter);
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));

        Assert.Equal([40, 62, 140, 62, 40, 50, 140, 50], SavedNumbers(document, "QuadPoints"));
    }

    [Fact]
    public void PageBoxes_EmitLowerLeftThenUpperRight()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.Letter);
        page.SetContent(Encoding.ASCII.GetBytes("BT (b) Tj ET"));
        page.MediaBox = PdfRect.FromSize(0, 0, 612, PageHeight);
        page.TrimBox = PdfRect.FromSize(20, 30, 555, 700);

        Assert.Equal([0, 0, 612, 792], SavedNumbers(document, "MediaBox"));
        Assert.Equal([20, 30, 575, 730], SavedNumbers(document, "TrimBox"));
    }

    private static double[] SavedNumbers(PortableDocument document, string key)
    {
        var reader = DocumentReader.Parse(document.ToArray());
        var node = ContentTestHelpers.Kid(reader, 0);
        var array = Assert.IsType<ArrayObject>(reader.Resolve(Find(reader, node, key)));
        return [.. array.Select(value => Assert.IsType<NumberObject>(reader.Resolve(value)).DoubleValue)];
    }

    private static DocumentObject Find(DocumentReader reader, DictionaryObject page, string key)
    {
        if (page.ContainsKey(key))
        {
            return page[key];
        }

        var annotations = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(annotations[0]));
        return annotation[key];
    }
}

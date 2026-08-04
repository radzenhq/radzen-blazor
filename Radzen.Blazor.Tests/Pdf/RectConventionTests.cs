#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

        var emission = Emit(document);
        var annotation = FirstAnnotation(emission);

        Carries("square annotation", "/Rect [10 20 110 70]", annotation);
    }

    [Fact]
    public void MarkupQuadPoints_PutTheTopEdgeFirst()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.Letter);
        page.Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(40, 50, 100, 12)));

        var emission = Emit(document);
        var annotation = FirstAnnotation(emission);

        Carries("highlight annotation", "/QuadPoints [40 62 140 62 40 50 140 50]", annotation);
    }

    [Fact]
    public void PageBoxes_EmitLowerLeftThenUpperRight()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.Letter);
        page.SetContent(Encoding.ASCII.GetBytes("BT (b) Tj ET"));
        page.MediaBox = PdfRect.FromSize(0, 0, 612, PageHeight);
        page.TrimBox = PdfRect.FromSize(20, 30, 555, 700);

        var node = Line(Emit(document), "/Type /Page ");

        Carries("page", "/MediaBox [0 0 612 792]", node);
        Carries("page", "/TrimBox [20 30 575 730]", node);
    }

    private static string FirstAnnotation(string emission)
        => IndirectObject(emission, References("page", "Annots", 1, Line(emission, "/Type /Page "))[0]);
}

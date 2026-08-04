#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 Table 164: annotation /C and /IC hold 0, 1, 3 or 4 numbers (no colour, grayscale, RGB, CMYK).
public class AnnotationColorSpaceTests
{
    private static byte[] FileWithAnnotationColors()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R /Annots [5 0 R 6 0 R 7 0 R 8 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 5 >>\nstream\nBT ET\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Highlight /Rect [10 10 110 30] "
            + "/QuadPoints [10 30 110 30 10 10 110 10] /C [0] >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 40 110 90] "
            + "/C [0 1 1 0] /IC [] >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 100 110 150] "
            + "/C [] /IC [0 0 0 1] >>\nendobj\n");
        pdf.Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 160 110 210] "
            + "/C [1 0 0] /IC [0.5] >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 9\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(5)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(6)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(7)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(8)))
            .Append("trailer\n<< /Size 9 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    [Fact]
    public void Load_GrayscaleAnnotationColor_ExpandsToRgb()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[0];

        Assert.Equal(Color.FromRgb(0, 0, 0), Assert.IsType<HighlightAnnotation>(annotation).Color);
    }

    [Fact]
    public void Load_CmykAnnotationColor_ConvertsToRgb()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[1];

        Assert.Equal(Color.FromRgb(255, 0, 0), Assert.IsType<SquareAnnotation>(annotation).Color);
    }

    [Fact]
    public void Load_EmptyInteriorColor_MeansNoFill()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[1];

        Assert.Null(Assert.IsType<SquareAnnotation>(annotation).InteriorColor);
    }

    [Fact]
    public void Load_CmykInteriorColor_ConvertsToRgb()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[2];

        Assert.Equal(Color.FromRgb(0, 0, 0), Assert.IsType<SquareAnnotation>(annotation).InteriorColor);
    }

    [Fact]
    public void Load_GrayscaleInteriorColor_ExpandsToRgb()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[3];

        Assert.Equal(Color.FromRgb(128, 128, 128), Assert.IsType<SquareAnnotation>(annotation).InteriorColor);
    }

    [Fact]
    public void Load_RgbAnnotationColor_IsUnchanged()
    {
        var annotation = Load(FileWithAnnotationColors()).Pages[0].Annotations[3];

        Assert.Equal(Color.FromRgb(255, 0, 0), Assert.IsType<SquareAnnotation>(annotation).Color);
    }

    private static byte[] TwoComponentAnnotationColor()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R /Annots [5 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 5 >>\nstream\nBT ET\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 10 110 30] /C [0 1] >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(5)))
            .Append("trailer\n<< /Size 6 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Load_TwoComponentAnnotationColor_IsRetainedAsUnmodeled()
    {
        var loaded = Load(TwoComponentAnnotationColor());

        Assert.Empty(loaded.Pages[0].Annotations);

        var emission = Encoding.Latin1.GetString(loaded.ToArray());
        References("page", "Annots", 1, Line(emission, "/Type /Page "));
    }
}

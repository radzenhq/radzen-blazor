#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A loaded page whose /MediaBox has a non-zero origin must re-save with that origin
// intact - content coordinates are preserved verbatim, so dropping the origin shifts
// the visible content.
public class MediaBoxOriginTests
{
    private static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] OnePageWithBox(string box)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox " + box + " /Contents 4 0 R >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Length 12 >>\nstream\n0 0 10 10 re\nendstream\nendobj\n");
        return Wrap(pdf, 5);
    }

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    private static double N(ArrayObject box, int i) => Assert.IsType<NumberObject>(box[i]).DoubleValue;

    [Fact]
    public void NonZeroOrigin_RoundTrips()
    {
        var loaded = Load(OnePageWithBox("[20 20 612 812]"));

        var reader = DocumentReader.Parse(loaded.ToArray());
        var box = Assert.IsType<ArrayObject>(reader.Resolve(DocumentLoadTests.Kid(reader, 0)["MediaBox"]));

        Assert.Equal(20, N(box, 0), 0.01);
        Assert.Equal(20, N(box, 1), 0.01);
        Assert.Equal(612, N(box, 2), 0.01);
        Assert.Equal(812, N(box, 3), 0.01);
    }

    [Fact]
    public void ZeroOrigin_RoundTrips()
    {
        var loaded = Load(OnePageWithBox("[0 0 400 500]"));

        var reader = DocumentReader.Parse(loaded.ToArray());
        var box = Assert.IsType<ArrayObject>(reader.Resolve(DocumentLoadTests.Kid(reader, 0)["MediaBox"]));

        Assert.Equal(0, N(box, 0), 0.01);
        Assert.Equal(0, N(box, 1), 0.01);
        Assert.Equal(400, N(box, 2), 0.01);
        Assert.Equal(500, N(box, 3), 0.01);
    }
}

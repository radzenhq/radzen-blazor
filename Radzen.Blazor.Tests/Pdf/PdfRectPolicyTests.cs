#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// One /Rect reader, three malformed-input policies. The policies differ on purpose - each
// call site keeps the one it had when they were three separate readers - so they are pinned
// here rather than left to whichever file the reader happened to live in.
public class PdfRectPolicyTests
{
    private static readonly RectPolicy Strict = RectPolicy.Strict("missing", "non-numeric");

    // The reader only has to resolve loose objects here, so the fixture is a minimal
    // one-page file plus whatever extra objects a test wants to reference.
    private static DocumentReader Reader(params string[] extra)
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        for (var i = 0; i < extra.Length; i++)
        {
            pdf.Object(4 + i, extra[i]);
        }

        var count = 4 + extra.Length;
        var xref = pdf.Position;
        pdf.Append($"xref\n0 {count}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append($"trailer\n<< /Size {count} /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return DocumentReader.Parse(pdf.ToArray());
    }

    private static ArrayObject Array(params DocumentObject[] values)
    {
        var array = new ArrayObject();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static ArrayObject Numbers(params double[] values)
        => Array(System.Array.ConvertAll(values, static v => (DocumentObject)new NumberObject(v)));

    [Fact]
    public void ReadsCornersInAnyOrderAsNormalisedPdfSpaceBounds()
    {
        var reader = Reader();
        foreach (var array in new[] { Numbers(10, 20, 110, 70), Numbers(110, 70, 10, 20) })
        {
            var bounds = PdfRects.Read(reader, array, Strict);
            Assert.Equal(10, bounds.Left);
            Assert.Equal(20, bounds.Bottom);
            Assert.Equal(110, bounds.Right);
            Assert.Equal(70, bounds.Top);
            Assert.Equal(100, bounds.Width);
            Assert.Equal(50, bounds.Height);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    public void StrictRequiresExactlyFourNumbers(int count)
    {
        var array = Numbers([.. new double[count]]);
        Assert.Equal("missing", Assert.Throws<DocumentParseException>(() => PdfRects.Read(Reader(), array, Strict)).Message);
    }

    [Fact]
    public void StrictRejectsAMissingArrayAndANonNumericCoordinate()
    {
        var reader = Reader();
        Assert.Equal("missing", Assert.Throws<DocumentParseException>(() => PdfRects.Read(reader, null, Strict)).Message);

        var array = Array(new NumberObject(0), new NameObject("Bad"), new NumberObject(2), new NumberObject(3));
        Assert.Equal("non-numeric", Assert.Throws<DocumentParseException>(() => PdfRects.Read(reader, array, Strict)).Message);
    }

    [Fact]
    public void ZeroFallbackReadsAMissingOrShortArrayAsEmpty()
    {
        var reader = Reader();
        foreach (var array in new ArrayObject?[] { null, Numbers(1, 2, 3) })
        {
            var bounds = PdfRects.Read(reader, array, RectPolicy.ZeroFallback);
            Assert.Equal(0, bounds.Left);
            Assert.Equal(0, bounds.Bottom);
            Assert.Equal(0, bounds.Width);
            Assert.Equal(0, bounds.Height);
        }
    }

    [Fact]
    public void DefaultSizeReadsAMissingOrShortArrayAsTheGivenSize()
    {
        var reader = Reader();
        foreach (var array in new ArrayObject?[] { null, Numbers(1, 2, 3) })
        {
            var bounds = PdfRects.Read(reader, array, RectPolicy.DefaultSize(200, 14));
            Assert.Equal(200, bounds.Width);
            Assert.Equal(14, bounds.Height);
        }
    }

    // A short array falls back wholesale, but a four-element array with one unusable
    // coordinate keeps the three that did resolve and reads the odd one out as zero.
    [Fact]
    public void FallbackPoliciesReadANonNumericCoordinateAsZero()
    {
        var array = Array(new NumberObject(10), new NumberObject(20), new NameObject("Bad"), new NumberObject(70));
        var bounds = PdfRects.Read(Reader(), array, RectPolicy.DefaultSize(200, 14));
        Assert.Equal(0, bounds.Left);
        Assert.Equal(20, bounds.Bottom);
        Assert.Equal(10, bounds.Right);
        Assert.Equal(70, bounds.Top);
    }

    // A /Rect may legally state coordinates as indirect references; unresolved they read as
    // zero and collapse the rectangle, so resolution is part of the shared reader's contract.
    [Fact]
    public void ResolvesIndirectCoordinates()
    {
        var reader = Reader("4 0 obj\n110\nendobj\n");
        var array = Array(new NumberObject(10), new NumberObject(20), new ReferenceObject(4, 0), new NumberObject(70));
        var bounds = PdfRects.Read(reader, array, Strict);
        Assert.Equal(110, bounds.Right);
        Assert.Equal(100, bounds.Width);
    }
}

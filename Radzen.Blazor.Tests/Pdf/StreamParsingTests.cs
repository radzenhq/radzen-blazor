using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Stream-object contract (ISO 32000-1 7.3.8).
public class StreamParsingTests
{
    private static string Data(StreamObject stream) => Encoding.Latin1.GetString(stream.Data.ToArray());

    private static byte[] Build(FixturePdf pdf, int count)
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

    [Fact]
    public void DirectLength_ExtractsDataExactly()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Length 5 /Type /Custom >>\nstream\nHello\nendstream\nendobj\n");
        var reader = DocumentReader.Parse(Build(pdf, 2));

        var stream = Assert.IsType<StreamObject>(reader.GetObject(1));
        Assert.Equal("Hello", Data(stream));
        Assert.Equal("Custom", Assert.IsType<NameObject>(stream.Dictionary["Type"]).Value);
    }

    [Fact]
    public void DirectLength_PreservesEmbeddedNewline()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Length 3 >>\nstream\na\nb\nendstream\nendobj\n");
        var reader = DocumentReader.Parse(Build(pdf, 2));

        var stream = Assert.IsType<StreamObject>(reader.GetObject(1));
        Assert.Equal("a\nb", Data(stream));
    }

    [Fact]
    public void StreamKeyword_CrLfAccepted()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Length 5 >>\nstream\r\nHello\r\nendstream\nendobj\n");
        var reader = DocumentReader.Parse(Build(pdf, 2));

        Assert.Equal("Hello", Data(Assert.IsType<StreamObject>(reader.GetObject(1))));
    }

    [Fact]
    public void StreamKeyword_LfAccepted()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Length 5 >>\nstream\nHello\nendstream\nendobj\n");
        var reader = DocumentReader.Parse(Build(pdf, 2));

        Assert.Equal("Hello", Data(Assert.IsType<StreamObject>(reader.GetObject(1))));
    }

    [Fact]
    public void IndirectLength_ResolvedFromOtherObject()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Length 2 0 R >>\nstream\nHello\nendstream\nendobj\n")
            .Object(2, "2 0 obj\n5\nendobj\n");
        var reader = DocumentReader.Parse(Build(pdf, 3));

        var stream = Assert.IsType<StreamObject>(reader.GetObject(1));
        Assert.Equal(5, stream.Data.Length);
        Assert.Equal("Hello", Data(stream));
    }
}

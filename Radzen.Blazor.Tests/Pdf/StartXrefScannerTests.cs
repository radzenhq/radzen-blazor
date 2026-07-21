#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class StartXrefScannerTests
{
    [Fact]
    public void FindStartXref_ReadsTheOffset()
    {
        var data = Encoding.Latin1.GetBytes("%PDF-1.7\nstartxref\n1234\n%%EOF");

        Assert.Equal(1234, PdfBytes.FindStartXref(data));
    }

    [Fact]
    public void FindStartXref_RejectsOverlongOffsetAsParseFailure()
    {
        var data = Encoding.Latin1.GetBytes("startxref\n" + new string('9', 25) + "\n%%EOF");

        Assert.Throws<DocumentParseException>(() => PdfBytes.FindStartXref(data));
    }
}

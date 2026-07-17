#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// An overlay is spliced in at the previous element's End, which is the byte immediately after
// its operator. Without a separator the two fuse into one unreadable token ("Tj" + "q" = "Tjq"),
// destroying both the original operator and the overlay's.
public class ReemitSeparatorTests
{
    private static byte[] Source()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (hello) Tj ET\n");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + "/Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 5; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static Document Watermarked()
    {
        using var input = new MemoryStream(Source());
        var document = Document.LoadFromStream(input);
        document.AddWatermark("DRAFT");
        return document;
    }

    [Fact]
    public void FlushedContent_DoesNotFuseTheOverlayOntoThePrecedingOperator()
    {
        var document = Watermarked();
        var text = document.Pages[0].ExtractText();

        Assert.Contains("hello", text);
    }

    [Fact]
    public void ReadingBeforeSaving_DoesNotDestroyPageContent()
    {
        var document = Watermarked();
        document.Pages[0].ExtractText();

        using var output = new MemoryStream();
        document.SaveToStream(output);
        output.Position = 0;

        var reloaded = Document.LoadFromStream(output);
        Assert.Contains("hello", reloaded.Pages[0].ExtractText());
    }
}

#nullable enable

using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A content-stream comment (ISO 32000-1 7.2.4) may sit between a show operator's string
// operand and the operator keyword. The PreserveAdvance TJ-rewrite must find the operator
// past that comment rather than reject the stream as malformed.
public class TextReplacerCommentReparseTests
{
    // /F0 gives A width 200 and B width 900, so replacing A with B changes the advance and
    // forces the [...] TJ rewrite that has to locate the Tj operator after the comment.
    private static Document LoadedWidthDocumentWithComment()
    {
        const string streamData = "BT /F0 10 Tf 72 700 Td (A) % kern\nTj (Z) Tj ET";
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Custom "
                + "/Encoding /WinAnsiEncoding /FirstChar 65 /LastChar 90 /Widths ["
                + "200 900 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 500] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return Document.LoadFromStream(input);
    }

    [Fact]
    public void ReplaceText_PreserveAdvance_AcrossComment_RewritesShow()
    {
        var loaded = LoadedWidthDocumentWithComment();

        var count = loaded.ReplaceText("A", "B");
        var reloaded = InterpreterTestSupport.Load(loaded.ToArray());

        Assert.Equal(1, count);
        Assert.Contains("B", reloaded.ExtractText());
        Assert.Contains("Z", reloaded.ExtractText());
    }
}

#nullable enable

using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class OpaqueOperatorPassthroughTests
{
    private const string StreamData = "q /GS0 gs 1 J 1 j 4 M 10 10 m 200 10 l S Q";

    private static byte[] Fixture()
    {
        var obj4 = $"4 0 obj\n<< /Length {StreamData.Length} >>\nstream\n{StreamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /ExtGState << /GS0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, obj4)
            .Object(5, "5 0 obj\n<< /Type /ExtGState /ca 0.5 >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void FullReencode_CarriesUnmodeledOperators()
    {
        using var stream = new MemoryStream(Fixture());
        var document = PortableDocument.LoadFromStream(stream);
        var page = document.Pages[0];

        var path = page.Content.OfType<PathContent>().First();
        path.Thickness = 3;

        var content = Encoding.Latin1.GetString(InterpreterTestSupport.PageContentBytes(document.ToArray(), 0));

        Assert.Contains("/GS0 gs", content);
        Assert.Contains("1 J", content);
        Assert.Contains("1 j", content);
        Assert.Contains("4 M", content);
    }
}

#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderRepairRobustnessTests
{
    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    private static string Offset(FixturePdf pdf, int number)
        => pdf.OffsetOf(number).ToString(CultureInfo.InvariantCulture);

    [Fact]
    public void CorruptFlateXrefStream_TriggersRepairAndLoadsPages()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 3 >>\nstream\n0 g\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /XRef /W [1 2 1] /Size 6 /Root 1 0 R /Filter /FlateDecode /Length 8 >>\nstream\nGARBAGE!\nendstream\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 5) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Equal(1, document.Pages.Count);
        Assert.Equal(Encoding.ASCII.GetBytes("0 g"), document.Pages[0].GetContent());
    }

    [Fact]
    public void CorruptFlateXrefStream_ExtractsTextAfterRepair()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R"
            + " /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        var content = "BT /F1 12 Tf 72 700 Td (Recovered) Tj ET";
        pdf.Object(4, "4 0 obj\n<< /Length " + content.Length.ToString(CultureInfo.InvariantCulture)
            + " >>\nstream\n" + content + "\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /XRef /W [1 2 1] /Size 7 /Root 1 0 R /Filter /FlateDecode /Length 4 >>\nstream\nBAD!\nendstream\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 6) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Contains("Recovered", document.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void Repair_TwoCatalogs_PicksNewestDeterministically()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Page /Parent 6 0 R /MediaBox [0 0 300 400] >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /Pages /Kids [3 0 R 7 0 R] /Count 2 >>\nendobj\n");
        pdf.Object(8, "8 0 obj\n<< /Type /Catalog /Pages 6 0 R >>\nendobj\n");
        pdf.Append("startxref\n" + Offset(pdf, 1) + "\n%%EOF\n");

        var document = Load(pdf.ToArray());

        Assert.Equal(2, document.Pages.Count);
        Assert.Equal(300.0, document.Pages[1].Width.Point, 0.01);
        Assert.Equal(400.0, document.Pages[1].Height.Point, 0.01);
    }

    [Fact]
    public void CyclicStreamLength_DoesNotOverflowStack()
    {
        var pdf = new FixturePdf();
        pdf.Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 4 0 R >>\nstream\n(hello)\nendstream\nendobj\n");
        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 5\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n"
            + xrefOffset.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

        var exception = Record.Exception(() =>
        {
            var document = Load(pdf.ToArray());
            var content = document.Pages[0].GetContent();
            if (content is not null)
            {
                Assert.Equal(Encoding.ASCII.GetBytes("(hello)"), content);
            }
        });

        Assert.True(exception is null or DocumentParseException,
            $"Cyclic /Length must be rejected as a parse error, but threw {exception}");
    }
}

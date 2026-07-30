#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReversedPageBoxLoadTests
{
    private static byte[] Build(string mediaBox, string pageExtra)
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (loaded) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox " + mediaBox + " /Contents 4 0 R "
            + pageExtra + " >>\nendobj\n");
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

    private static Page Load(string mediaBox, string pageExtra = "")
        => PortableDocument.LoadFromStream(new MemoryStream(Build(mediaBox, pageExtra))).Pages[0];

    [Fact]
    public void ReversedMediaBox_NormalizesToPositiveSize()
    {
        var page = Load("[612 792 0 0]");

        Assert.Equal(612.0, page.MediaBox.Width);
        Assert.Equal(792.0, page.MediaBox.Height);
        Assert.Equal(612.0, page.Width.Point);
        Assert.Equal(792.0, page.Height.Point);
    }

    [Fact]
    public void ReversedCropBox_NormalizesToPositiveSize()
    {
        var page = Load("[0 0 612 792]", "/CropBox [200 400 10 20]");

        Assert.NotNull(page.CropBox);
        Assert.Equal(190.0, page.CropBox!.Value.Width);
        Assert.Equal(380.0, page.CropBox!.Value.Height);
    }

    [Fact]
    public void AscendingMediaBox_KeepsSize()
    {
        var page = Load("[0 0 612 792]");

        Assert.Equal(612.0, page.MediaBox.Width);
        Assert.Equal(792.0, page.MediaBox.Height);
    }
}

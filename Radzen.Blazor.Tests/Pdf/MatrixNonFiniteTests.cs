#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class MatrixNonFiniteTests
{
    private static byte[] FileWithContent(string content)
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, $"4 0 obj\n<< /Length {content.Length} >>\nstream\n{content}endstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i <= 4; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    // ISO 32000-1 7.3.3 reals carry no exponent, so the lexer drops "1.0e400" as malformed.
    private static string Huge => new string('9', 400) + ".0";

    [Fact]
    public void Load_OverflowingCmOperand_ProducesNonFiniteTransform()
    {
        var document = Load(FileWithContent($"q {Huge} 0 0 1 0 0 cm 10 10 100 50 re f Q\n"));

        var path = document.Pages[0].Content.OfType<PathContent>().Single();

        Assert.True(double.IsInfinity(path.Transform.A), $"Transform.A was {path.Transform.A:R}");
    }

    [Fact]
    public void LoadSave_UntouchedNonFiniteCm_RoundTripsUnchanged()
    {
        var source = FileWithContent($"q {Huge} 0 0 1 0 0 cm 10 10 100 50 re f Q\n");
        var document = Load(source);

        var saved = document.ToArray();

        Assert.Contains(Huge, Encoding.Latin1.GetString(saved));
    }

    [Fact]
    public void Author_NonFiniteTransform_ThrowsWhenWritten()
    {
        var document = new Document();
        var page = document.Pages.Add();
        var path = page.Content.Add(new PathContent { Fill = true, Transform = Matrix.Scale(double.NaN, 1) });
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.Close();

        var error = Assert.Throws<InvalidOperationException>(() => document.ToArray());

        Assert.Equal("A PDF number cannot be NaN or infinite.", error.Message);
    }
}

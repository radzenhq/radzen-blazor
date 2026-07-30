#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class PendingContentEditFlushTests
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
    public void PageFindText_AfterWatermark_FindsOverlayText()
    {
        Assert.NotEmpty(Watermarked().Pages[0].FindText("DRAFT"));
    }

    [Fact]
    public void DocumentFindText_AfterWatermark_FindsOverlayText()
    {
        var hits = Watermarked().FindText("DRAFT");

        Assert.NotEmpty(hits);
        Assert.Equal(0, hits[0].PageIndex);
    }

    [Fact]
    public void DocumentFindText_AfterWatermark_AgreesWithPageFindText()
    {
        var document = Watermarked();

        var documentHits = document.FindText("DRAFT").Count;

        Assert.Equal(document.Pages[0].FindText("DRAFT").Count, documentHits);
    }


    [Fact]
    public void ReplaceText_AfterWatermark_ReplacesOverlayText()
    {
        var document = Watermarked();

        Assert.NotEqual(0, document.Pages[0].ReplaceText("DRAFT", "FINAL"));
        Assert.Contains("FINAL", document.Pages[0].ExtractText());
    }

    [Fact]
    public void ExtractText_AfterWatermark_IncludesOverlayText()
    {
        Assert.Contains("DRAFT", Watermarked().Pages[0].ExtractText());
    }

    [Fact]
    public void ExtractPositionedText_AfterWatermark_IncludesOverlayText()
    {
        Assert.Contains(Watermarked().Pages[0].ExtractPositionedText(), run => run.Text.Contains("DRAFT"));
    }
}

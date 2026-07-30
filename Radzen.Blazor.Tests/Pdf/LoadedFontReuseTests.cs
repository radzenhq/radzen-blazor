#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadedFontReuseTests
{
    private static byte[] SharedFontDocument(int pages)
    {
        var kids = "";
        for (var i = 0; i < pages; i++)
        {
            kids += (4 + i) + " 0 R ";
        }

        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [" + kids + "] /Count " + pages
                + " /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        for (var i = 0; i < pages; i++)
        {
            pdf.Object(4 + i, (4 + i) + " 0 obj\n<< /Type /Page /Parent 2 0 R >>\nendobj\n");
        }

        var count = 4 + pages;
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    [Fact]
    public void SharedFontDictionary_IsReversedOncePerDocument()
    {
        var document = Load(SharedFontDocument(3));

        var first = document.Pages[0].TextFonts!["F1"];
        Assert.Same(first, document.Pages[1].TextFonts!["F1"]);
        Assert.Same(first, document.Pages[2].TextFonts!["F1"]);
    }

    [Fact]
    public void SeparateDocuments_DoNotShareReversedFonts()
    {
        var first = Load(SharedFontDocument(1));
        var second = Load(SharedFontDocument(1));

        Assert.NotSame(first.Pages[0].TextFonts!["F1"], second.Pages[0].TextFonts!["F1"]);
    }

    [Fact]
    public void AppendedPage_ReusesTheDonorReversedFont()
    {
        var source = Load(SharedFontDocument(2));
        var target = new PortableDocument();
        target.Append(source);

        Assert.Same(source.Pages[0].TextFonts!["F1"], target.Pages[0].TextFonts!["F1"]);
        Assert.Same(source.Pages[0].TextFonts!["F1"], target.Pages[1].TextFonts!["F1"]);
    }
}

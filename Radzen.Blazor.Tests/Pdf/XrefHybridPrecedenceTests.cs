#nullable enable
using System;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.5.8.4: an object not found in the standard section is sought in /XRefStm before /Prev.
// The current standard section wins over /XRefStm; /XRefStm only supersedes /Prev.
public class XrefHybridPrecedenceTests
{
    private static byte[] ConflictingHybridFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");

        var decoyOffset = pdf.Position;
        pdf.Append("7 0 obj\n<< /Tag /Decoy >>\nendobj\n");

        var correctOffset = pdf.Position;
        pdf.Mark(7);
        pdf.Append("7 0 obj\n<< /Tag /Classic >>\nendobj\n");

        var offset6 = pdf.Position;
        var payload = FixturePdf.XrefStreamEntry(1, (int)decoyOffset, 0);
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /XRef /Size 8 /Index [7 1] /W [1 2 1] /Root 1 0 R /Length 4 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n");

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("7 1\n")
            .Append(FixturePdf.Entry20(correctOffset))
            .Append("trailer\n<< /Size 8 /Root 1 0 R /XRefStm " + offset6 + " >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void ClassicSectionEntryWinsOverXRefStm()
    {
        var reader = DocumentReader.Parse(ConflictingHybridFile());

        var obj = Assert.IsType<DictionaryObject>(reader.GetObject(7));
        Assert.Equal("Classic", Assert.IsType<NameObject>(obj["Tag"]).Value);
    }
}

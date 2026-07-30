#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class InfoIndirectReferenceImportTests
{
    private static byte[] BuildPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 12 >>\nstream\n0 0 10 10 re\nendstream\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Title (Original) /RadzenRef 7 0 R >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n(secret-value)\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 8; i++)
        {
            if (i == 5)
            {
                pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
                continue;
            }

            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R /Info 6 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void FullSave_ImportsIndirectInfoValueWithoutDangling()
    {
        var document = Document.LoadFromStream(new MemoryStream(BuildPdf()));

        var saved = document.ToArray();
        var reader = DocumentReader.Parse(saved);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]));

        var reference = Assert.IsType<ReferenceObject>(info["RadzenRef"]);
        var resolved = Assert.IsType<StringObject>(reader.Resolve(reference));
        Assert.Equal("secret-value", resolved.Value);
    }
}

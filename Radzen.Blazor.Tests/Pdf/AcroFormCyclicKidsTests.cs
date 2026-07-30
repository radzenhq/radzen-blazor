#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AcroFormCyclicKidsTests
{
    private static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] CyclicFormSource()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (parent) /FT /Tx /Kids [6 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /T (child) /Parent 5 0 R /Kids [5 0 R] >>\nendobj\n");
        return Wrap(pdf, 7);
    }

    private static byte[] NestedFormSource()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 7 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (address) /FT /Tx /Kids [6 0 R 7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /T (city) /Parent 5 0 R /Rect [100 700 350 720] >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Widget /T (zip) /Parent 5 0 R /Rect [100 660 350 680] >>\nendobj\n");
        return Wrap(pdf, 8);
    }

    [Fact]
    public void CyclicKids_ThrowsInsteadOfStackOverflow()
    {
        Assert.Throws<DocumentParseException>(
            () => PortableDocument.LoadFromStream(new MemoryStream(CyclicFormSource())));
    }

    [Fact]
    public void NestedTree_EnumeratesAllTerminals()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(NestedFormSource()));

        Assert.NotNull(document.AcroForm);
        Assert.Equal(2, document.AcroForm!.FieldNames.Count);
        Assert.Contains("address.city", document.AcroForm.FieldNames);
        Assert.Contains("address.zip", document.AcroForm.FieldNames);
    }
}

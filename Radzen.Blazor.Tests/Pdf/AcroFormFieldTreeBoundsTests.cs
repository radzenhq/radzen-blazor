#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AcroFormFieldTreeBoundsTests
{
    private static byte[] DeepKidsSource(int levels)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n");

        for (var level = 0; level < levels; level++)
        {
            var number = 5 + level;
            var body = new StringBuilder();
            body.Append(number).Append(" 0 obj\n<< /T (f").Append(level).Append(") /FT /Tx");

            if (level == levels - 1)
            {
                body.Append(" /V (leaf) /Type /Annot /Subtype /Widget /Rect [100 700 350 720]");
            }
            else
            {
                body.Append(" /Kids [").Append(number + 1).Append(" 0 R]");
            }

            body.Append(" >>\nendobj\n");
            pdf.Object(number, body.ToString());
        }

        return FixturePdf.Wrap(pdf, 5 + levels);
    }

    private static byte[] SharedNodeSource()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 6 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (a) /Kids [7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /T (b) /Kids [7 0 R] >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /T (shared) /FT /Tx /V (v) >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 8);
    }

    [Fact]
    public void DeepFieldTree_BeyondConfiguredDepth_Throws()
    {
        var limits = new ReaderLimits { MaxPageTreeDepth = 6 };
        var exception = Assert.Throws<DocumentParseException>(
            () => PortableDocument.LoadFromStream(new MemoryStream(DeepKidsSource(40)), limits));

        Assert.Contains("deep", exception.Message);
    }

    [Fact]
    public void DeepFieldTree_WithinConfiguredDepth_Loads()
    {
        var limits = new ReaderLimits { MaxPageTreeDepth = 64 };
        var document = PortableDocument.LoadFromStream(new MemoryStream(DeepKidsSource(40)), limits);

        Assert.Single(document.AcroForm!.Fields);
        Assert.Equal("leaf", document.AcroForm!.Fields[0].Value);
    }

    [Fact]
    public void SharedFieldNode_IsDiagnosedNotRevisited()
    {
        Assert.Throws<DocumentParseException>(
            () => PortableDocument.LoadFromStream(new MemoryStream(SharedNodeSource())));
    }
}

#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AcroFormParentChainDepthTests
{
    private static byte[] DeepChainSource(int levels)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots ["
                + (4 + levels) + " 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n");

        for (var level = 0; level < levels; level++)
        {
            var number = 5 + level;
            var body = new StringBuilder();
            body.Append(number).Append(" 0 obj\n<< /T (f").Append(level).Append(')');

            if (level == 0)
            {
                body.Append(" /FT /Tx /V (deep)");
            }
            else
            {
                body.Append(" /Parent ").Append(number - 1).Append(" 0 R");
            }

            if (level == levels - 1)
            {
                body.Append(" /Type /Annot /Subtype /Widget /Rect [100 700 350 720]");
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

    private static byte[] CyclicParentSource()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [7 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (root) /FT /Tx /V (v) /Kids [7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /T (a) /Parent 7 0 R >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /T (b) /Parent 6 0 R /Type /Annot /Subtype /Widget /Rect [100 700 350 720] >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 8);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(40)]
    public void DeepParentChain_InheritsValueFromRoot(int levels)
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(DeepChainSource(levels)));
        var field = document.AcroForm!.Fields.Single();

        Assert.Equal("deep", field.Value);
    }

    [Fact]
    public void CyclicParentChain_ThrowsInsteadOfTruncatingSilently()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(CyclicParentSource()));
        var field = document.AcroForm!.Fields.Single();

        var exception = Assert.Throws<DocumentParseException>(() => field.Value);
        Assert.Contains("Cyclic", exception.Message);
    }
}

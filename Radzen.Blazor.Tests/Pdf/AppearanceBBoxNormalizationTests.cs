#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class AppearanceBBoxNormalizationTests
{
    private const string YesAppearance = "1 0 0 RG 2 2 16 16 re S";

    private static byte[] CheckBoxWithBBox(string bbox)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 7 0 R /Resources 8 0 R /Annots [6 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /FT /Btn /T (agree) /Kids [6 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 700 120 720] "
                + "/AP << /N << /Yes 9 0 R >> >> /AS /Yes >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Length 3 >>\nstream\nq Q\nendstream\nendobj\n")
            .Object(8, "8 0 obj\n<< >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /XObject /Subtype /Form /BBox " + bbox + " /Length " + YesAppearance.Length
                + " >>\nstream\n" + YesAppearance + "\nendstream\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 10\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < 10; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 10 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static ContentOperation FlattenedTransform(string bbox)
    {
        var document = Document.LoadFromStream(new MemoryStream(CheckBoxWithBBox(bbox)));
        document.Flatten();
        var reader = FormTestSupport.Reload(document);

        var page = FormTestSupport.FirstPage(reader);
        var pdf = new StringBuilder();
        var contents = reader.Resolve(page["Contents"]);
        if (contents is StreamObject single)
        {
            pdf.Append(FormTestSupport.Decode(single));
        }
        else
        {
            foreach (var entry in Assert.IsType<ArrayObject>(contents))
            {
                pdf.Append(FormTestSupport.Decode(Assert.IsType<StreamObject>(reader.Resolve(entry)))).Append('\n');
            }
        }

        return ContentStreamTokenizer.Parse(Encoding.Latin1.GetBytes(pdf.ToString()))
            .Last(operation => operation.Operator == "cm");
    }

    [Fact]
    public void ForwardBBox_PaintsWithAnUprightTransform()
    {
        var cm = FlattenedTransform("[0 0 20 20]");

        Assert.Equal(1, cm.Num(0));
        Assert.Equal(1, cm.Num(3));
        Assert.Equal(100, cm.Num(4));
        Assert.Equal(700, cm.Num(5));
    }

    [Fact]
    public void ReversedBBox_AgreesWithTheForwardBBoxInsteadOfMirroring()
    {
        var cm = FlattenedTransform("[20 20 0 0]");

        Assert.Equal(1, cm.Num(0));
        Assert.Equal(1, cm.Num(3));
        Assert.Equal(100, cm.Num(4));
        Assert.Equal(700, cm.Num(5));
    }
}

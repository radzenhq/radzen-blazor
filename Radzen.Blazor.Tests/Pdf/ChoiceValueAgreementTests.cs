using Radzen.Documents.Pdf;
using System.IO;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// FormField.Value reports a multi-select choice field's /V and FormFlattener paints it.
// They render the same array through one primitive, so they cannot disagree.
public class ChoiceValueAgreementTests
{
    private static byte[] MultiSelectListForm(string values)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Ch /T (colors) /P 3 0 R "
                + "/Rect [100 640 280 700] /Opt [(Red) (Green) (Blue)] /V " + values + " >>\nendobj\n");
        return Wrap(pdf, 6);
    }

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

    [Theory]
    [InlineData("[(Red) (Blue)]", "Red, Blue")]
    [InlineData("[(Red)]", "Red")]
    [InlineData("(Red)", "Red")]
    public void ValueReportsWhatFlattenPaints(string stored, string expected)
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm(stored)));
        Assert.Equal(expected, document.AcroForm!.Fields.Single().Value);

        document.Flatten();
        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));

        Assert.Contains("(" + expected + ") Tj", content);
    }

    // A newline separator would reach the painter as a raw byte inside a single Tj literal,
    // where PDF does not treat it as a line break: the join must stay renderable on one line.
    [Fact]
    public void PaintedSelectionsCarryNoRawNewline()
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm("[(Red) (Blue)]")));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        var tj = content[content.IndexOf("(Red")..content.IndexOf(") Tj")];

        Assert.DoesNotContain('\n', tj);
    }
}

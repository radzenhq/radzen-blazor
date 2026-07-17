using Radzen.Documents.Pdf;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// FormField.Value reports a multi-select choice field's /V as a display string. That is a
// data API and "Red, Blue" is a fine answer for it. The flattener paints *page content*, and
// a list box does not render as that string: ISO 32000-1 12.7.4.4 stacks every /Opt entry
// with the selected ones highlighted. The two paths agreeing never made the painting right,
// so Value is pinned here and the rendering is pinned in FlattenBakeFidelityTests.
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
    public void ValueReportsTheSelectionAsADisplayString(string stored, string expected)
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm(stored)));

        Assert.Equal(expected, document.AcroForm!.Fields.Single().Value);
    }

    // What Value reports is not what the page must show. An array /V is a multi-selection: it
    // renders as stacked highlighted /Opt entries, so "Red, Blue" is a string the field never
    // shows and Green is dropped. The rendering lives in the /AP the flattener would discard,
    // so it refuses rather than paint that string.
    [Theory]
    [InlineData("[(Red) (Blue)]")]
    [InlineData("[(Red)]")]
    public void FlattenRefusesToPaintAMultiSelectionAsThatString(string stored)
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm(stored)));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // A scalar /V is a single selection, which is the one line the flattener paints.
    [Fact]
    public void FlattenPaintsASingleSelection()
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm("(Red)")));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));

        Assert.Contains("(Red) Tj", content);
    }
}

using Radzen.Documents.Pdf;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 12.7.4.4: a list box renders every /Opt entry stacked with the selected ones highlighted.
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
        return FixturePdf.Wrap(pdf, 6);
    }

    [Theory]
    [InlineData("[(Red) (Blue)]", "Red, Blue")]
    [InlineData("[(Red)]", "Red")]
    [InlineData("(Red)", "Red")]
    public void ValueReportsTheSelectionAsADisplayString(string stored, string expected)
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiSelectListForm(stored)));

        Assert.Equal(expected, document.AcroForm!.Fields.Single().Value);
    }

    [Theory]
    [InlineData("[(Red) (Blue)]")]
    [InlineData("[(Red)]")]
    public void FlattenRefusesToPaintAMultiSelectionAsThatString(string stored)
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiSelectListForm(stored)));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenPaintsASingleSelection()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiSelectListForm("(Red)")));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));

        Assert.Contains("(Red) Tj", content);
    }
}

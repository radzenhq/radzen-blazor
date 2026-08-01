#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class FlattenBakeFidelityTests
{
    private static byte[] TextForm(string extra, string value)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (secret) /P 3 0 R "
                + "/Rect [100 640 280 700] " + extra + " /V (" + value + ") >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 6);
    }

    private static byte[] ChoiceForm(string extra, string values)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Ch /T (colors) /P 3 0 R "
                + "/Rect [100 640 280 700] " + extra + " /Opt [(Red) (Green) (Blue)] /V " + values + " >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 6);
    }

    private static PortableDocument Load(byte[] bytes) => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    [Fact]
    public void FlattenRefusesPasswordFieldRatherThanPaintCleartext()
    {
        var document = Load(TextForm("/Ff 8192", "hunter2"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenNeverPaintsAPasswordValueOntoThePage()
    {
        var document = Load(TextForm("/Ff 8192", "hunter2"));

        try
        {
            document.Flatten();
        }
        catch (NotSupportedException)
        {
            return;
        }

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.DoesNotContain("hunter2", content);
    }

    [Fact]
    public void FlattenRefusesMultilineField()
    {
        var document = Load(TextForm("/Ff 4096", "first"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenRefusesCombField()
    {
        var document = Load(TextForm("/Ff 16777216", "ABCD"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenRefusesCenteredQuadding()
    {
        var document = Load(TextForm("/Q 1", "centered"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenRefusesRightQuadding()
    {
        var document = Load(TextForm("/Q 2", "right"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenRefusesNonWinAnsiValue()
    {
        var document = Load(TextForm(string.Empty, @"\376\377\116\055"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenStillBakesAPlainTextField()
    {
        var document = Load(TextForm("/Ff 0 /Q 0", "Sofia"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Sofia) Tj", content);
    }

    [Fact]
    // ISO 32000-1 12.7.4.4: a list box renders each /Opt entry stacked with selected ones highlighted; a single-line bake cannot express that.
    public void FlattenRefusesMultiSelectListBox()
    {
        var document = Load(ChoiceForm(string.Empty, "[(Red) (Blue)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenRefusesArrayValuedListBoxWithOneSelection()
    {
        var document = Load(ChoiceForm(string.Empty, "[(Red)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenBakesAScalarValuedListBox()
    {
        var document = Load(ChoiceForm(string.Empty, "(Red)"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Red) Tj", content);
    }

    [Fact]
    public void FlattenBakesAComboBox()
    {
        var document = Load(ChoiceForm("/Ff 131072", "(Red)"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Red) Tj", content);
    }

    [Fact]
    public void FlattenRefusesMultiSelectComboBox()
    {
        var document = Load(ChoiceForm("/Ff 131072", "[(Red) (Blue)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenDropsAnEmptyPasswordFieldWithoutAppearance()
    {
        var document = Load(TextForm("/Ff 8192", string.Empty));
        document.Flatten();

        var reader = FormTestSupport.Reload(document);
        var page = FormTestSupport.FirstPage(reader);

        Assert.False(page.TryGetValue("Annots", out var annots)
            && reader.Resolve(annots!) is ArrayObject array && array.Count > 0);
        Assert.False(page.ContainsKey("Contents"));
    }
}

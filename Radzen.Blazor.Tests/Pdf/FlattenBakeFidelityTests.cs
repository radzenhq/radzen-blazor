#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Flatten bakes a single left-aligned WinAnsi line. AcroForm.CanBakeAppearance already
// refuses that bake for a multiline/password/comb field or a non-zero /Q; the flattener
// must refuse the same shapes rather than paint a wrong - or password-leaking - line.
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
        return Wrap(pdf, 6);
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

    private static Document Load(byte[] bytes) => Document.LoadFromStream(new MemoryStream(bytes));

    // The headline defect: /Ff 8192 is a password field whose value must never render as
    // cleartext page content.
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

    // /Ff 4096 is multiline: the bake would collapse every line onto one.
    [Fact]
    public void FlattenRefusesMultilineField()
    {
        var document = Load(TextForm("/Ff 4096", "first"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // /Ff 16777216 is comb: the bake would lose the evenly spaced cells.
    [Fact]
    public void FlattenRefusesCombField()
    {
        var document = Load(TextForm("/Ff 16777216", "ABCD"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // /Q 1 is centered: the bake left-aligns, jumping the text.
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

    // A value the baked WinAnsi line cannot encode would be painted as wrong glyphs.
    // \376\377 is the UTF-16BE BOM of a PDF text string; \116\055 is U+4E2D.
    [Fact]
    public void FlattenRefusesNonWinAnsiValue()
    {
        var document = Load(TextForm(string.Empty, @"\376\377\116\055"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // A plain left-aligned single-line text field is exactly what the bake is faithful to.
    [Fact]
    public void FlattenStillBakesAPlainTextField()
    {
        var document = Load(TextForm("/Ff 0 /Q 0", "Sofia"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Sofia) Tj", content);
    }

    // ISO 32000-1 12.7.4.4: a list box renders every /Opt entry stacked vertically with the
    // selected ones highlighted. The single-line bake cannot express that, so it must refuse
    // rather than paint a lossy "Red, Blue" and discard the /AP that renders it correctly.
    [Fact]
    public void FlattenRefusesMultiSelectListBox()
    {
        var document = Load(ChoiceForm(string.Empty, "[(Red) (Blue)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // An array /V is the multi-select storage form even when one entry is selected.
    [Fact]
    public void FlattenRefusesArrayValuedListBoxWithOneSelection()
    {
        var document = Load(ChoiceForm(string.Empty, "[(Red)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // A scalar /V is a single selection, which is the one line this paints. Radzen's own
    // authored list box stores /V this way and bakes a single-line /AP for it.
    [Fact]
    public void FlattenBakesAScalarValuedListBox()
    {
        var document = Load(ChoiceForm(string.Empty, "(Red)"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Red) Tj", content);
    }

    // /Ff 131072 is a combo box: it does render its value as one left-aligned line.
    [Fact]
    public void FlattenBakesAComboBox()
    {
        var document = Load(ChoiceForm("/Ff 131072", "(Red)"));
        document.Flatten();

        var content = FormTestSupport.PageContentText(FormTestSupport.Reload(document));
        Assert.Contains("(Red) Tj", content);
    }

    // A combo box holds one value; a multi-select combo is still not one line.
    [Fact]
    public void FlattenRefusesMultiSelectComboBox()
    {
        var document = Load(ChoiceForm("/Ff 131072", "[(Red) (Blue)]"));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    // Refusal must not depend on the value being present: an empty field paints nothing
    // either way, and deleting it loses no rendering.
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

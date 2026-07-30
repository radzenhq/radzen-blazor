#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AcroFormAppearanceTests
{
    private static byte[] TextForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [6 0 R] /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 5 0 R >> >> >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Name /Helv >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V () /P 3 0 R /Rect [100 700 350 720] /DA (/Helv 8 Tf 0 g) >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 7);
    }

    private static byte[] CheckboxForm(string onState)
    {
        const string glyph = "0 0 18 18 re f";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Btn /T (Agree) /V /Off /AS /Off /P 3 0 R /Rect [100 660 118 678] /AP << /N << /" + onState + " 6 0 R /Off 7 0 R >> >> >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 18 18] /Length " + glyph.Length + " >>\nstream\n" + glyph + "\nendstream\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 18 18] /Length 0 >>\nstream\n\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 8);
    }

    private static PortableDocument Load(byte[] bytes) => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    private static bool HasAppearanceStream(DocumentReader reader, DictionaryObject widget)
        => widget.TryGetValue("AP", out var ap) && reader.Resolve(ap!) is DictionaryObject dict
            && dict.TryGetValue("N", out var n)
            && reader.Resolve(n!) is StreamObject or DictionaryObject;

    [Fact]
    public void FillCyrillicValueStoresUtf16ValueWithoutLossyAppearance()
    {
        var document = Load(TextForm());
        document.AcroForm!.FillField("Name", "Атанас Костов");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Name");

        Assert.True(field.TryGetValue("V", out var value));
        var stored = Assert.IsType<StringObject>(reader.Resolve(value!)).Value;
        var raw = Encoding.Latin1.GetBytes(stored);
        Assert.Equal(0xFE, raw[0]);
        Assert.Equal(0xFF, raw[1]);
        Assert.Equal("Атанас Костов", Encoding.BigEndianUnicode.GetString(raw, 2, raw.Length - 2));

        var form = FormTestSupport.AcroForm(reader);
        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && reader.Resolve(need!) is BooleanObject flag && flag.Value);
        Assert.False(HasAppearanceStream(reader, field));
    }

    [Fact]
    public void FillAsciiValueHonorsDefaultAppearanceFontSize()
    {
        var document = Load(TextForm());
        document.AcroForm!.FillField("Name", "ACME");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Name");
        var appearance = FormTestSupport.NormalAppearanceText(reader, field);

        Assert.Contains("ACME", appearance);
        Assert.Contains("8 Tf", appearance);
        Assert.DoesNotContain("12 Tf", appearance);
    }

    [Fact]
    public void CheckFieldUsesOnStateNamedByAppearanceDictionary()
    {
        var document = Load(CheckboxForm("On"));
        document.AcroForm!.CheckField("Agree");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Agree");

        Assert.Equal("On", FormTestSupport.NameValue(reader, field, "V"));
        Assert.Equal("On", FormTestSupport.NameValue(reader, field, "AS"));
    }

    [Fact]
    public void CheckFieldUsesYesWhenAppearanceProvidesYesState()
    {
        var document = Load(CheckboxForm("Yes"));
        document.AcroForm!.CheckField("Agree");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Agree");

        Assert.Equal("Yes", FormTestSupport.NameValue(reader, field, "V"));
        Assert.Equal("Yes", FormTestSupport.NameValue(reader, field, "AS"));
    }
}

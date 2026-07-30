#nullable enable
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class HierarchicalFormFieldTests
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

    private static byte[] HierarchicalForm()
    {
        const string stale = "/Tx BMC q (STALE) Tj Q EMC";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 8 0 R 9 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 9 0 R] /NeedAppearances true /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 7 0 R >> >> >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (address) /Kids [6 0 R 10 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (city) /V () /P 3 0 R /Rect [100 700 350 720] /DA (/Helv 12 Tf 0 g) >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Name /Helv >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /Annot /Subtype /Widget /P 3 0 R /Parent 10 0 R /Rect [100 660 350 680] /AP << /N 11 0 R >> >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V () /P 3 0 R /Rect [100 600 350 620] /DA (/Helv 12 Tf 0 g) >>\nendobj\n")
            .Object(10, "10 0 obj\n<< /FT /Tx /T (zip) /V () /Kids [8 0 R] >>\nendobj\n")
            .Object(11, "11 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 250 20] /Length " + stale.Length + " >>\nstream\n" + stale + "\nendstream\nendobj\n");
        return Wrap(pdf, 12);
    }

    private static PortableDocument Load()
        => PortableDocument.LoadFromStream(new MemoryStream(HierarchicalForm()));

    private static DictionaryObject FieldByPath(DocumentReader reader, params string[] path)
    {
        var form = FormTestSupport.AcroForm(reader);
        var current = (ArrayObject)reader.Resolve(form["Fields"]);
        DictionaryObject match = null!;
        foreach (var part in path)
        {
            match = null!;
            foreach (var entry in current)
            {
                var dict = (DictionaryObject)reader.Resolve(entry);
                if (dict.TryGetValue("T", out var t) && reader.Resolve(t!) is StringObject name && name.Value == part)
                {
                    match = dict;
                    break;
                }
            }

            Assert.NotNull(match);
            if (match.TryGetValue("Kids", out var kids))
            {
                current = (ArrayObject)reader.Resolve(kids!);
            }
        }

        return match;
    }

    [Fact]
    public void QualifiedTerminalNamesAreEnumerableAndParentIsHidden()
    {
        var document = Load();
        var names = document.AcroForm!.Fields.Select(f => f.Name).ToArray();

        Assert.Contains("address.city", document.AcroForm.FieldNames);
        Assert.Contains("address.zip", document.AcroForm.FieldNames);
        Assert.Contains("Name", document.AcroForm.FieldNames);
        Assert.DoesNotContain("address", document.AcroForm.FieldNames);
        Assert.Equal(3, document.AcroForm.FieldNames.Count);
        Assert.Equal(3, names.Length);
        Assert.Equal(document.AcroForm.FieldNames, names);
    }

    [Fact]
    public void FieldNameRoundTripsThroughFillField()
    {
        var document = Load();
        var form = document.AcroForm!;

        foreach (var field in form.Fields)
        {
            form.FillField(field.Name, "filled " + field.Name);
        }

        Assert.Equal(["filled address.city", "filled address.zip", "filled Name"], form.Fields.Select(f => f.Value));
    }

    [Fact]
    public void PartialNameExposesTheOwnTitleOfANestedField()
    {
        var document = Load();
        var fields = document.AcroForm!.Fields;

        Assert.Equal(["city", "zip", "Name"], fields.Select(f => f.PartialName));
        Assert.Equal(["address.city", "address.zip", "Name"], fields.Select(f => f.Name));
    }

    [Fact]
    public void FillQualifiedFieldSetsValueOnTerminal()
    {
        var document = Load();
        document.AcroForm!.FillField("address.zip", "90210");

        var reader = FormTestSupport.Reload(document);
        var zip = FieldByPath(reader, "address", "zip");

        Assert.True(zip.TryGetValue("V", out var value));
        Assert.Equal("90210", Assert.IsType<StringObject>(reader.Resolve(value!)).Value);
    }

    [Fact]
    public void FillSeparateWidgetFieldRefreshesWidgetAppearance()
    {
        var document = Load();
        document.AcroForm!.FillField("address.zip", "90210");

        var reader = FormTestSupport.Reload(document);
        var zip = FieldByPath(reader, "address", "zip");
        var widget = (DictionaryObject)reader.Resolve(((ArrayObject)reader.Resolve(zip["Kids"]))[0]);
        var appearance = FormTestSupport.NormalAppearanceText(reader, widget);

        Assert.Contains("90210", appearance);
        Assert.DoesNotContain("STALE", appearance);
    }

    [Fact]
    public void FillMergedWidgetFieldRegeneratesOwnAppearance()
    {
        var document = Load();
        document.AcroForm!.FillField("address.city", "Sofia");

        var reader = FormTestSupport.Reload(document);
        var city = FieldByPath(reader, "address", "city");

        Assert.Equal("Sofia", city.Value(reader, "V"));
        Assert.Contains("Sofia", FormTestSupport.NormalAppearanceText(reader, city));
    }
}

internal static class HierarchicalFormFieldTestExtensions
{
    public static string? Value(this DictionaryObject dict, DocumentReader reader, string key)
        => dict.TryGetValue(key, out var value) && reader.Resolve(value!) is StringObject text ? text.Value : null;
}

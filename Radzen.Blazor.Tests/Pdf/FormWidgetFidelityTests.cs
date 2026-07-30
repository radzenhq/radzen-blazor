#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FormWidgetFidelityTests
{

    private static byte[] InheritedTypeForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 7 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (group) /FT /Btn /Kids [6 0 R 7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /T (first) /Parent 5 0 R /P 3 0 R /Rect [100 700 120 720] /AP << /N << /Yes 8 0 R /Off 8 0 R >> >> /AS /Off >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Widget /T (second) /Parent 5 0 R /P 3 0 R /Rect [100 660 120 680] >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length 4 >>\nstream\nq\nQ\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 9);
    }

    [Fact]
    // ISO 32000-1 12.7.3.1: /FT is inheritable, so a terminal kid reports its parent's field type.
    public void FieldTypeReadsTheInheritedFieldType()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(InheritedTypeForm()));

        Assert.All(document.AcroForm!.Fields, field => Assert.Equal(FormFieldType.Button, field.Type));
    }

    [Fact]
    public void InheritedTypeAgreesWithTheMutatorGuard()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(InheritedTypeForm()));
        var field = document.AcroForm!.Fields[0];

        Assert.Equal(FormFieldType.Button, field.Type);
        document.AcroForm.CheckField(field.Name);
    }


    private static byte[] MultiWidgetForm()
    {
        const string stale = "/Tx BMC q (STALE) Tj Q EMC";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 7 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /FT /Tx /T (name) /V () /DA (/Helv 12 Tf 0 g) /Kids [6 0 R 7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 700 350 720] /AP << /N 8 0 R >> >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 600 350 620] /AP << /N 8 0 R >> >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 250 20] /Length " + stale.Length + " >>\nstream\n" + stale + "\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 9);
    }

    private static DictionaryObject Kid(DocumentReader reader, DictionaryObject field, int index)
        => (DictionaryObject)reader.Resolve(((ArrayObject)reader.Resolve(field["Kids"]))[index]);

    [Fact]
    public void FillFieldRefreshesEveryWidgetOfAField()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiWidgetForm()));
        document.AcroForm!.FillField("name", "Sofia");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "name");

        for (var index = 0; index < 2; index++)
        {
            var appearance = FormTestSupport.NormalAppearanceText(reader, Kid(reader, field, index));
            Assert.Contains("Sofia", appearance);
            Assert.DoesNotContain("STALE", appearance);
        }
    }

    [Fact]
    public void EachWidgetBakesItsOwnAppearanceBox()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiWidgetForm()));
        document.AcroForm!.FillField("name", "Sofia");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "name");
        var first = (DictionaryObject)reader.Resolve(Kid(reader, field, 0)["AP"]);
        var second = (DictionaryObject)reader.Resolve(Kid(reader, field, 1)["AP"]);

        Assert.NotSame(reader.Resolve(first["N"]), reader.Resolve(second["N"]));
    }

    private static byte[] MultiWidgetCheckBoxForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 7 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /FT /Btn /T (agree) /Kids [6 0 R 7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 700 120 720] /AP << /N << /Yes 8 0 R /Off 8 0 R >> >> /AS /Off >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Widget /Parent 5 0 R /P 3 0 R /Rect [100 660 120 680] /AP << /N << /Yes 8 0 R /Off 8 0 R >> >> /AS /Off >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length 4 >>\nstream\nq\nQ\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 9);
    }

    [Fact]
    public void CheckFieldTurnsOnEveryWidgetOfAField()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultiWidgetCheckBoxForm()));
        document.AcroForm!.CheckField("agree");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "agree");

        Assert.Equal("Yes", FormTestSupport.NameValue(reader, Kid(reader, field, 0), "AS"));
        Assert.Equal("Yes", FormTestSupport.NameValue(reader, Kid(reader, field, 1), "AS"));
    }


    private static byte[] PushButtonForm(bool withAppearance)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n");

        if (withAppearance)
        {
            pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Btn /Ff 65536 /T (submit) /P 3 0 R /Rect [100 700 200 730] /AP << /N 6 0 R >> >>\nendobj\n")
                .Object(6, "6 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 100 30] /Length 4 >>\nstream\nq\nQ\nendstream\nendobj\n");
            return FixturePdf.Wrap(pdf, 7);
        }

        pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Btn /Ff 65536 /T (submit) /P 3 0 R /Rect [100 700 200 730] >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 6);
    }

    [Fact]
    public void FlattenRefusesVisiblePushButtonAppearance()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(PushButtonForm(withAppearance: true)));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenDropsPushButtonWithoutAppearance()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(PushButtonForm(withAppearance: false)));
        document.Flatten();

        var reader = FormTestSupport.Reload(document);
        var page = FormTestSupport.FirstPage(reader);

        Assert.False(page.TryGetValue("Annots", out var annots)
            && reader.Resolve(annots!) is ArrayObject array && array.Count > 0);
    }

    private static byte[] PushButtonFormWithStateAppearance()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Btn /Ff 65536 /T (submit) /P 3 0 R /Rect [100 700 200 730] /AP << /N << /Off 6 0 R >> >> >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 100 30] /Length 4 >>\nstream\nq\nQ\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 7);
    }

    [Fact]
    public void FlattenRefusesPushButtonWithVisibleStateDictionaryAppearance()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(PushButtonFormWithStateAppearance()));

        Assert.Throws<NotSupportedException>(document.Flatten);
    }
}

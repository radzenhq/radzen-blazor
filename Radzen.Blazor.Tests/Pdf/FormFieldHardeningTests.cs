#nullable enable
using System;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class FormFieldHardeningTests
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

    private static Document BuildTextForm(Action<TextFieldDefinition> configure)
    {
        var document = new Radzen.Documents.Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Form", "Helvetica");
        var renderer = new DocumentRenderer();
        var pdf = renderer.Render(document);

        var field = new TextFieldDefinition("field") { X = 100, Y = 700, Width = 180, Height = 20 };
        configure(field);
        pdf.FormFields.Add(field);

        return Document.LoadFromStream(new MemoryStream(pdf.ToArray()));
    }

    private static string? OptionalAppearanceText(DocumentReader reader, DictionaryObject field)
    {
        if (!field.TryGetValue("AP", out var apObject) || reader.Resolve(apObject!) is not DictionaryObject ap
            || !ap.TryGetValue("N", out var nObject))
        {
            return null;
        }

        var normal = reader.Resolve(nObject!);
        if (normal is StreamObject stream)
        {
            return FormTestSupport.Decode(stream);
        }

        if (normal is DictionaryObject states)
        {
            foreach (var key in states.Keys)
            {
                if (reader.Resolve(states[key]) is StreamObject stateStream)
                {
                    return FormTestSupport.Decode(stateStream);
                }
            }
        }

        return null;
    }


    [Fact]
    public void FillFieldRejectsButtonField()
    {
        var document = FormTestSupport.LoadFixture();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.FillField("Agree", "x"));
    }

    [Fact]
    public void CheckFieldRejectsTextField()
    {
        var document = FormTestSupport.LoadFixture();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.CheckField("Name"));
    }

    [Fact]
    public void SelectOptionRejectsTextField()
    {
        var document = FormTestSupport.LoadFixture();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.SelectOption("Name", "x"));
    }

    [Fact]
    public void SelectRadioOptionRejectsTextField()
    {
        var document = FormTestSupport.LoadFixture();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.SelectRadioOption("Name", "x"));
    }


    [Fact]
    public void FillPasswordFieldDoesNotBakeValueIntoAppearance()
    {
        var document = BuildTextForm(field => field.Password = true);
        document.AcroForm!.FillField("field", "hunter2");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "field");

        Assert.Equal("hunter2", Assert.IsType<StringObject>(reader.Resolve(field["V"])).Value);
        var form = FormTestSupport.AcroForm(reader);
        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && Assert.IsType<BooleanObject>(reader.Resolve(need!)).Value);

        var appearance = OptionalAppearanceText(reader, field);
        Assert.True(appearance is null || !appearance.Contains("hunter2"));
    }

    [Fact]
    public void FillMultilineFieldDefersAppearanceToViewer()
    {
        var document = BuildTextForm(field => field.Multiline = true);
        document.AcroForm!.FillField("field", "line one\nline two");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "field");
        var form = FormTestSupport.AcroForm(reader);

        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && Assert.IsType<BooleanObject>(reader.Resolve(need!)).Value);
        Assert.Equal("line one\nline two", Assert.IsType<StringObject>(reader.Resolve(field["V"])).Value);
    }

    [Fact]
    public void FillCombFieldDefersAppearanceToViewer()
    {
        var document = BuildTextForm(field =>
        {
            field.Comb = true;
            field.MaxLength = 6;
        });
        document.AcroForm!.FillField("field", "ABC123");

        var reader = FormTestSupport.Reload(document);
        var form = FormTestSupport.AcroForm(reader);

        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && Assert.IsType<BooleanObject>(reader.Resolve(need!)).Value);
    }


    private static byte[] IndirectRectForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (amount) /P 3 0 R /Rect [6 0 R 7 0 R 8 0 R 9 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n100\nendobj\n")
            .Object(7, "7 0 obj\n700\nendobj\n")
            .Object(8, "8 0 obj\n300\nendobj\n")
            .Object(9, "9 0 obj\n720\nendobj\n");
        return Wrap(pdf, 10);
    }

    [Fact]
    public void FillFieldResolvesIndirectRectForAppearanceBox()
    {
        var document = Document.LoadFromStream(new MemoryStream(IndirectRectForm()));
        document.AcroForm!.FillField("amount", "42");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "amount");
        var ap = (DictionaryObject)reader.Resolve(field["AP"]);
        var appearance = (StreamObject)reader.Resolve(ap["N"]);
        var bbox = (ArrayObject)reader.Resolve(appearance.Dictionary["BBox"]);

        Assert.True(((NumberObject)reader.Resolve(bbox[2])).DoubleValue > 0.0);
        Assert.True(((NumberObject)reader.Resolve(bbox[3])).DoubleValue > 0.0);
    }


    private static byte[] DuplicateNameForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R 6 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 6 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (dup) /P 3 0 R /Rect [100 700 300 720] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (dup) /P 3 0 R /Rect [100 660 300 680] >>\nendobj\n");
        return Wrap(pdf, 7);
    }

    [Fact]
    public void DuplicateRootNamesAreBothReachable()
    {
        var document = Document.LoadFromStream(new MemoryStream(DuplicateNameForm()));
        var form = document.AcroForm!;

        Assert.Equal(2, form.Fields.Count);
        Assert.Equal(["dup", "dup_2"], form.FieldNames);
        Assert.Equal(["dup", "dup_2"], form.Fields.Select(f => f.Name));
        Assert.Equal(["dup", "dup"], form.Fields.Select(f => f.PartialName));
        Assert.NotSame(form.Fields[0].Dictionary, form.Fields[1].Dictionary);

        form.FillField(form.Fields[0].Name, "AAA");
        form.FillField(form.Fields[1].Name, "BBB");

        Assert.Equal("AAA", form.Fields[0].Value);
        Assert.Equal("BBB", form.Fields[1].Value);
    }


    private static byte[] MultiSelectListForm()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Ch /T (colors) /P 3 0 R /Rect [100 640 280 700] /Opt [(Red) (Green) (Blue)] /V [(Red) (Blue)] >>\nendobj\n");
        return Wrap(pdf, 6);
    }

    [Fact]
    // ISO 32000-1 12.7.4.4: a multi-select list box (/V array) renders as stacked highlighted /Opt entries, so flatten refuses to join it into one line.
    public void FlattenRefusesMultiSelectListBoxSelections()
    {
        var document = Document.LoadFromStream(new MemoryStream(MultiSelectListForm()));
        document.AcroForm!.Fields.Single();

        Assert.Throws<NotSupportedException>(document.Flatten);
    }


    private static byte[] SignatureForm(bool withAppearance)
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] >>\nendobj\n");

        if (withAppearance)
        {
            pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Sig /T (sig) /P 3 0 R /Rect [100 700 260 760] /AP << /N 6 0 R >> >>\nendobj\n")
                .Object(6, "6 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 160 60] /Length 4 >>\nstream\nq\nQ\nendstream\nendobj\n");
            return Wrap(pdf, 7);
        }

        pdf.Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Sig /T (sig) /P 3 0 R /Rect [0 0 0 0] >>\nendobj\n");
        return Wrap(pdf, 6);
    }

    [Fact]
    public void FlattenRefusesVisibleSignatureAppearance()
    {
        var document = Document.LoadFromStream(new MemoryStream(SignatureForm(withAppearance: true)));
        Assert.Throws<NotSupportedException>(document.Flatten);
    }

    [Fact]
    public void FlattenDropsUnsignedSignatureFieldWithoutAppearance()
    {
        var document = Document.LoadFromStream(new MemoryStream(SignatureForm(withAppearance: false)));
        document.Flatten();

        var reader = FormTestSupport.Reload(document);
        var page = FormTestSupport.FirstPage(reader);
        Assert.False(page.TryGetValue("Annots", out var annots)
            && reader.Resolve(annots!) is ArrayObject array && array.Count > 0);
    }


    [Fact]
    public void DuplicateAttachmentNamesThrowOnSave()
    {
        var document = new Radzen.Documents.Document();
        BuildTestSupport.AddText(document.Sections.Add(), "Body", "Helvetica");
        var renderer = new DocumentRenderer();
        renderer.Attachments.Add("data.xml", [1, 2, 3], AttachmentRelationship.Data, "text/xml");
        renderer.Attachments.Add("data.xml", [4, 5, 6], AttachmentRelationship.Data, "text/xml");

        Assert.Throws<InvalidOperationException>(() => renderer.Render(document).ToArray());
    }
}

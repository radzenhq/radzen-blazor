#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FormFieldCreationTests
{
    private static Document BuildDocument()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice", "Helvetica");
        return builder.Build();
    }

    private static Document WithFields()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Name")
        {
            X = 100,
            Y = 700,
            Width = 250,
            Height = 20,
            Value = "Radzen Ltd",
            Font = new Font { Name = "Helvetica", Size = 12 },
        });
        document.FormFields.Add(new CheckBoxFieldDefinition("Agree")
        {
            X = 100,
            Y = 660,
            Width = 18,
            Height = 18,
            Checked = true,
        });
        return document;
    }

    private static DocumentReader Reload(Document document)
        => DocumentReader.Parse(document.ToArray());

    private static double[] RectOf(DocumentReader reader, DictionaryObject widget)
    {
        Assert.True(widget.TryGetValue("Rect", out var rectObject));
        var rect = Assert.IsType<ArrayObject>(reader.Resolve(rectObject!));
        return [.. rect.Select(entry => ((NumberObject)reader.Resolve(entry)).DoubleValue)];
    }

    private static string AllPageContent(DocumentReader reader)
    {
        var page = FormTestSupport.FirstPage(reader);
        Assert.True(page.TryGetValue("Contents", out var contents), "page has no /Contents");
        var resolved = reader.Resolve(contents!);
        if (resolved is StreamObject stream)
        {
            return FormTestSupport.Decode(stream);
        }

        var parts = (ArrayObject)resolved;
        var text = new StringBuilder();
        foreach (var part in parts)
        {
            text.Append(FormTestSupport.Decode((StreamObject)reader.Resolve(part)));
        }

        return text.ToString();
    }

    [Fact]
    public void CreatedFieldsSaveIntoAcroFormFields()
    {
        var reader = Reload(WithFields());
        var form = FormTestSupport.AcroForm(reader);

        Assert.True(form.TryGetValue("Fields", out var fieldsObject));
        var fields = Assert.IsType<ArrayObject>(reader.Resolve(fieldsObject!));
        Assert.Equal(2, fields.Count);

        var name = FormTestSupport.Field(reader, "Name");
        Assert.Equal("Tx", FormTestSupport.NameValue(reader, name, "FT"));
        Assert.Equal([100, 700, 350, 720], RectOf(reader, name));
        var value = Assert.IsType<StringObject>(reader.Resolve(name["V"]));
        Assert.Equal("Radzen Ltd", value.Value);

        var agree = FormTestSupport.Field(reader, "Agree");
        Assert.Equal("Btn", FormTestSupport.NameValue(reader, agree, "FT"));
        Assert.Equal([100, 660, 118, 678], RectOf(reader, agree));
        Assert.Equal("Yes", FormTestSupport.NameValue(reader, agree, "V"));
        Assert.Equal("Yes", FormTestSupport.NameValue(reader, agree, "AS"));
    }

    [Fact]
    public void CreatedFormCarriesDefaultResourcesAndAppearances()
    {
        var reader = Reload(WithFields());
        var form = FormTestSupport.AcroForm(reader);

        var da = Assert.IsType<StringObject>(reader.Resolve(form["DA"]));
        Assert.Contains("Tf", da.Value);

        var dr = Assert.IsType<DictionaryObject>(reader.Resolve(form["DR"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(dr["Font"]));
        Assert.True(fonts.ContainsKey("Helv"));

        var name = FormTestSupport.Field(reader, "Name");
        var fieldDa = Assert.IsType<StringObject>(reader.Resolve(name["DA"]));
        Assert.Contains("12 Tf", fieldDa.Value);
        Assert.Contains("Radzen Ltd", FormTestSupport.NormalAppearanceText(reader, name));

        var agree = FormTestSupport.Field(reader, "Agree");
        var ap = Assert.IsType<DictionaryObject>(reader.Resolve(agree["AP"]));
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(ap["N"]));
        Assert.True(states.ContainsKey("Yes"));
        Assert.True(states.ContainsKey("Off"));
    }

    [Fact]
    public void CreatedWidgetsLandOnTheirPageAnnots()
    {
        var reader = Reload(WithFields());
        var page = FormTestSupport.FirstPage(reader);

        Assert.True(page.TryGetValue("Annots", out var annotsObject));
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(annotsObject!));
        var widgets = annots
            .Select(entry => (DictionaryObject)reader.Resolve(entry))
            .Where(annot => FormTestSupport.NameValue(reader, annot, "Subtype") == "Widget")
            .ToList();
        Assert.Equal(2, widgets.Count);
    }

    [Fact]
    public void ReloadedCreatedFieldsExposeValuesThroughAcroForm()
    {
        using var stream = new MemoryStream(WithFields().ToArray());
        var reloaded = Document.LoadFromStream(stream);

        Assert.NotNull(reloaded.AcroForm);
        var name = reloaded.AcroForm!.Fields.Single(f => f.Name == "Name");
        Assert.Equal(FormFieldType.Text, name.Type);
        Assert.Equal("Radzen Ltd", name.Value);
        var agree = reloaded.AcroForm.Fields.Single(f => f.Name == "Agree");
        Assert.Equal(FormFieldType.Button, agree.Type);
        Assert.Equal("Yes", agree.Value);
    }

    [Fact]
    public void FlattenAfterReloadDrawsValuesAndRemovesFields()
    {
        using var stream = new MemoryStream(WithFields().ToArray());
        var reloaded = Document.LoadFromStream(stream);
        reloaded.Flatten();

        Assert.Null(reloaded.AcroForm);

        var reader = Reload(reloaded);
        Assert.False(FormTestSupport.Catalog(reader).ContainsKey("AcroForm"));

        var page = FormTestSupport.FirstPage(reader);
        if (page.TryGetValue("Annots", out var annotsObject)
            && reader.Resolve(annotsObject!) is ArrayObject annots)
        {
            foreach (var entry in annots)
            {
                var annot = (DictionaryObject)reader.Resolve(entry);
                Assert.NotEqual("Widget", FormTestSupport.NameValue(reader, annot, "Subtype"));
            }
        }

        var content = AllPageContent(reader);
        Assert.Contains("Radzen Ltd", content);
        Assert.Contains("S\n", content);
    }

    [Fact]
    public void FlattenBeforeSaveDrawsCreatedFieldsStatically()
    {
        var document = WithFields();
        document.Flatten();

        Assert.Empty(document.FormFields);

        var reader = Reload(document);
        Assert.False(FormTestSupport.Catalog(reader).ContainsKey("AcroForm"));
        Assert.Contains("Radzen Ltd", AllPageContent(reader));
    }

    [Fact]
    public void DocumentWithoutCreatedFieldsSavesByteIdentically()
    {
        var baseline = BuildDocument().ToArray();

        var document = BuildDocument();
        Assert.Empty(document.FormFields);
        document.Flatten();

        Assert.Equal(baseline, document.ToArray());
    }

    [Fact]
    public void FlattenLeavesFilledLoadedFormStatic()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.FillField("Name", "Radzen Ltd");
        document.AcroForm.CheckField("Agree");
        document.Flatten();

        var reader = Reload(document);
        Assert.False(FormTestSupport.Catalog(reader).ContainsKey("AcroForm"));
        var content = AllPageContent(reader);
        Assert.Contains("Radzen Ltd", content);
    }
}

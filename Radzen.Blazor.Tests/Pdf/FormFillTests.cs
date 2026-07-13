#nullable enable
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FormFillTests
{
    [Fact]
    public void LoadedDocumentExposesAcroFormFields()
    {
        var document = FormTestSupport.LoadFixture();

        Assert.NotNull(document.AcroForm);
        var fields = document.AcroForm!.Fields;
        Assert.Equal(2, fields.Count);

        var name = fields.Single(f => f.Name == "Name");
        var agree = fields.Single(f => f.Name == "Agree");

        Assert.Equal(FormFieldType.Text, name.Type);
        Assert.Equal(FormFieldType.Button, agree.Type);
        Assert.True(string.IsNullOrEmpty(name.Value));
    }

    [Fact]
    public void FillTextFieldSetsValueOnReload()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.FillField("Name", "Radzen Ltd");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Name");

        Assert.True(field.TryGetValue("V", out var value));
        var text = Assert.IsType<StringObject>(reader.Resolve(value!));
        Assert.Equal("Radzen Ltd", text.Value);
    }

    [Fact]
    public void FillTextFieldRegeneratesAppearanceShowingValue()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.FillField("Name", "Radzen Ltd");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Name");
        var appearance = FormTestSupport.NormalAppearanceText(reader, field);

        Assert.Contains("Radzen Ltd", appearance);
        Assert.Contains("Tj", appearance);
    }

    [Fact]
    public void FilledTextFieldRoundTripsThroughPublicApi()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.FillField("Name", "Radzen Ltd");

        var reloaded = Document.LoadFromStream(new MemoryStream(FormTestSupport.Save(document)));

        Assert.NotNull(reloaded.AcroForm);
        var name = reloaded.AcroForm!.Fields.Single(f => f.Name == "Name");
        Assert.Equal("Radzen Ltd", name.Value);
    }

    [Fact]
    public void CheckboxStartsOffBeforeMutation()
    {
        var reader = DocumentReader.Parse(PdfTestResources.ReadAllBytes(FormTestSupport.Fixture));
        var field = FormTestSupport.Field(reader, "Agree");

        Assert.Equal("Off", FormTestSupport.NameValue(reader, field, "V"));
        Assert.Equal("Off", FormTestSupport.NameValue(reader, field, "AS"));
    }

    [Fact]
    public void CheckFieldSetsOnStateOnReload()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.CheckField("Agree");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Agree");

        var value = FormTestSupport.NameValue(reader, field, "V");
        var appearanceState = FormTestSupport.NameValue(reader, field, "AS");

        Assert.NotNull(value);
        Assert.NotNull(appearanceState);
        Assert.NotEqual("Off", value);
        Assert.NotEqual("Off", appearanceState);
        Assert.Equal(value, appearanceState);
    }

    [Fact]
    public void CheckFieldLeavesTextFieldUntouched()
    {
        var document = FormTestSupport.LoadFixture();
        document.AcroForm!.CheckField("Agree");

        var reader = FormTestSupport.Reload(document);
        var name = FormTestSupport.Field(reader, "Name");

        // The text field keeps its empty value; only the checkbox was mutated.
        Assert.False(name.TryGetValue("V", out var value) && reader.Resolve(value!) is StringObject text && text.Value.Length > 0);
    }
}

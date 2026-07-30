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

public class AcroFormChoiceRadioSelectionTests
{
    private static Document BuildLoadedForm()
    {
        var document = new Radzen.Documents.Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Survey", "Helvetica");
        var pdf = new DocumentRenderer().Render(document);

        var combo = new ChoiceFieldDefinition("Country")
        {
            X = 100, Y = 700, Width = 180, Height = 20, ComboBox = true,
        };
        combo.Options.Add("Bulgaria");
        combo.Options.Add("Germany");
        combo.Options.Add("Spain");
        pdf.FormFields.Add(combo);

        var list = new ChoiceFieldDefinition("Color")
        {
            X = 100, Y = 640, Width = 180, Height = 48,
        };
        list.Options.Add("Red");
        list.Options.Add("Green");
        list.Options.Add("Blue");
        pdf.FormFields.Add(list);

        var radio = new RadioGroupFieldDefinition("Size");
        radio.Options.Add(new RadioOptionDefinition("Small") { X = 100, Y = 600, Width = 16, Height = 16 });
        radio.Options.Add(new RadioOptionDefinition("Medium") { X = 100, Y = 570, Width = 16, Height = 16 });
        radio.Options.Add(new RadioOptionDefinition("Large") { X = 100, Y = 540, Width = 16, Height = 16 });
        pdf.FormFields.Add(radio);

        return Document.LoadFromStream(new MemoryStream(pdf.ToArray()));
    }

    private static double[] IndicesOf(DocumentReader reader, DictionaryObject field)
    {
        Assert.True(field.TryGetValue("I", out var indices), "field has no /I");
        var array = Assert.IsType<ArrayObject>(reader.Resolve(indices!));
        return [.. array.Select(entry => ((NumberObject)reader.Resolve(entry)).DoubleValue)];
    }

    [Fact]
    public void SelectOptionSetsComboBoxValueIndexAndAppearance()
    {
        var document = BuildLoadedForm();
        document.AcroForm!.SelectOption("Country", "Germany");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Country");

        Assert.Equal("Germany", Assert.IsType<StringObject>(reader.Resolve(field["V"])).Value);
        Assert.Equal([1], IndicesOf(reader, field));
        Assert.Contains("Germany", FormTestSupport.NormalAppearanceText(reader, field));
    }

    [Fact]
    public void SelectOptionSetsListBoxSelection()
    {
        var document = BuildLoadedForm();
        document.AcroForm!.SelectOption("Color", "Blue");

        var reader = FormTestSupport.Reload(document);
        var field = FormTestSupport.Field(reader, "Color");

        Assert.Equal("Blue", Assert.IsType<StringObject>(reader.Resolve(field["V"])).Value);
        Assert.Equal([2], IndicesOf(reader, field));
        Assert.Contains("Blue", FormTestSupport.NormalAppearanceText(reader, field));
    }

    [Fact]
    public void SelectOptionRejectsValueOutsideOptions()
    {
        var document = BuildLoadedForm();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.SelectOption("Country", "France"));
    }

    [Fact]
    public void SelectRadioOptionSetsParentValueAndKidAppearanceStates()
    {
        var document = BuildLoadedForm();
        document.AcroForm!.SelectRadioOption("Size", "Large");

        var reader = FormTestSupport.Reload(document);
        var group = FormTestSupport.Field(reader, "Size");

        Assert.Equal("Large", FormTestSupport.NameValue(reader, group, "V"));

        var kids = Assert.IsType<ArrayObject>(reader.Resolve(group["Kids"]));
        var states = kids
            .Select(entry => (DictionaryObject)reader.Resolve(entry))
            .Select(kid => FormTestSupport.NameValue(reader, kid, "AS"))
            .ToArray();
        Assert.Equal(["Off", "Off", "Large"], states);
    }

    [Fact]
    public void SelectRadioOptionRejectsUnknownOption()
    {
        var document = BuildLoadedForm();
        Assert.Throws<ArgumentException>(() => document.AcroForm!.SelectRadioOption("Size", "ExtraLarge"));
    }

    [Fact]
    public void SelectRadioOptionRoundTripsThroughPublicApi()
    {
        var document = BuildLoadedForm();
        document.AcroForm!.SelectRadioOption("Size", "Medium");

        var reloaded = Document.LoadFromStream(new MemoryStream(FormTestSupport.Save(document)));
        var size = reloaded.AcroForm!.Fields.Single(f => f.Name == "Size");
        Assert.Equal("Medium", size.Value);
    }
}

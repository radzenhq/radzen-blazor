#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RadioAndChoiceFieldTests
{
    private const int RadioFlag = 1 << 15;
    private const int ComboFlag = 1 << 17;

    private static Document BuildDocument()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Survey", "Helvetica");
        return builder.Build();
    }

    private static RadioGroupFieldDefinition RadioGroup()
    {
        var group = new RadioGroupFieldDefinition("Size") { SelectedValue = "Medium" };
        group.Options.Add(new RadioOptionDefinition("Small") { X = 100, Y = 700, Width = 16, Height = 16 });
        group.Options.Add(new RadioOptionDefinition("Medium") { X = 100, Y = 670, Width = 16, Height = 16 });
        group.Options.Add(new RadioOptionDefinition("Large") { X = 100, Y = 640, Width = 16, Height = 16 });
        return group;
    }

    private static ChoiceFieldDefinition Combo()
    {
        var combo = new ChoiceFieldDefinition("Country")
        {
            X = 100,
            Y = 600,
            Width = 180,
            Height = 20,
            ComboBox = true,
            Value = "Bulgaria",
        };
        combo.Options.Add("Bulgaria");
        combo.Options.Add("Germany");
        combo.Options.Add("Spain");
        return combo;
    }

    private static ChoiceFieldDefinition ListBox()
    {
        var list = new ChoiceFieldDefinition("Color")
        {
            X = 100,
            Y = 520,
            Width = 180,
            Height = 60,
            Value = "Green",
        };
        list.Options.Add("Red");
        list.Options.Add("Green");
        list.Options.Add("Blue");
        return list;
    }

    private static Document WithFields()
    {
        var document = BuildDocument();
        document.FormFields.Add(RadioGroup());
        document.FormFields.Add(Combo());
        document.FormFields.Add(ListBox());
        return document;
    }

    private static DocumentReader Reload(Document document)
        => DocumentReader.Parse(document.ToArray());

    private static int FlagsOf(DocumentReader reader, DictionaryObject field)
        => field.TryGetValue("Ff", out var ff) && reader.Resolve(ff!) is NumberObject flags ? flags.IntValue : 0;

    private static string[] OptionsOf(DocumentReader reader, DictionaryObject field)
    {
        Assert.True(field.TryGetValue("Opt", out var optObject), "field has no /Opt");
        var opt = Assert.IsType<ArrayObject>(reader.Resolve(optObject!));
        return [.. opt.Select(entry => ((StringObject)reader.Resolve(entry)).Value)];
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

    private static string AllPaintedContent(DocumentReader reader)
    {
        var text = new StringBuilder(AllPageContent(reader));
        var page = FormTestSupport.FirstPage(reader);
        if (reader.Resolve(page["Resources"]) is DictionaryObject resources
            && reader.GetDictionary(resources, "XObject") is { } xobjects)
        {
            foreach (var key in xobjects.Keys)
            {
                if (reader.Resolve(xobjects[key]) is StreamObject form)
                {
                    text.Append('\n').Append(FormTestSupport.Decode(form));
                }
            }
        }

        return text.ToString();
    }

    [Fact]
    public void RadioGroupSavesParentFieldWithKidWidgets()
    {
        var reader = Reload(WithFields());
        var group = FormTestSupport.Field(reader, "Size");

        Assert.Equal("Btn", FormTestSupport.NameValue(reader, group, "FT"));
        Assert.Equal(RadioFlag, FlagsOf(reader, group) & RadioFlag);
        Assert.Equal("Medium", FormTestSupport.NameValue(reader, group, "V"));
        Assert.Equal("Medium", FormTestSupport.NameValue(reader, group, "DV"));

        Assert.True(group.TryGetValue("Kids", out var kidsObject));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(kidsObject!));
        Assert.Equal(3, kids.Count);

        var states = new List<string?>();
        foreach (var entry in kids)
        {
            var kid = Assert.IsType<DictionaryObject>(reader.Resolve(entry));
            Assert.Equal("Widget", FormTestSupport.NameValue(reader, kid, "Subtype"));
            Assert.True(kid.TryGetValue("Parent", out _));
            states.Add(FormTestSupport.NameValue(reader, kid, "AS"));
        }

        Assert.Equal(["Off", "Medium", "Off"], states);
    }

    [Fact]
    public void RadioKidWidgetsCarryPerOptionAppearances()
    {
        var reader = Reload(WithFields());
        var group = FormTestSupport.Field(reader, "Size");
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(group["Kids"]));
        string[] values = ["Small", "Medium", "Large"];

        for (var i = 0; i < kids.Count; i++)
        {
            var kid = Assert.IsType<DictionaryObject>(reader.Resolve(kids[i]));
            var ap = Assert.IsType<DictionaryObject>(reader.Resolve(kid["AP"]));
            var normal = Assert.IsType<DictionaryObject>(reader.Resolve(ap["N"]));
            Assert.Equal(2, normal.Keys.Count());
            Assert.True(normal.ContainsKey(values[i]));
            Assert.True(normal.ContainsKey("Off"));

            var on = FormTestSupport.Decode(Assert.IsType<StreamObject>(reader.Resolve(normal[values[i]])));
            var off = FormTestSupport.Decode(Assert.IsType<StreamObject>(reader.Resolve(normal["Off"])));
            Assert.Contains("c\n", on);
            Assert.Contains("f\n", on);
            Assert.Contains("S\n", off);
            Assert.DoesNotContain("f\n", off);
        }
    }

    [Fact]
    public void RadioKidWidgetsLandOnTheirPageAnnots()
    {
        var reader = Reload(WithFields());
        var page = FormTestSupport.FirstPage(reader);

        Assert.True(page.TryGetValue("Annots", out var annotsObject));
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(annotsObject!));
        var widgets = annots
            .Select(entry => (DictionaryObject)reader.Resolve(entry))
            .Where(annot => FormTestSupport.NameValue(reader, annot, "Subtype") == "Widget")
            .ToList();

        Assert.Equal(5, widgets.Count);
        Assert.Equal(3, widgets.Count(w => w.ContainsKey("Parent")));

        var form = FormTestSupport.AcroForm(reader);
        var fields = Assert.IsType<ArrayObject>(reader.Resolve(form["Fields"]));
        Assert.Equal(3, fields.Count);
    }

    [Fact]
    public void ComboBoxSavesChoiceFieldWithComboFlag()
    {
        var reader = Reload(WithFields());
        var combo = FormTestSupport.Field(reader, "Country");

        Assert.Equal("Ch", FormTestSupport.NameValue(reader, combo, "FT"));
        Assert.Equal(ComboFlag, FlagsOf(reader, combo) & ComboFlag);
        Assert.Equal(["Bulgaria", "Germany", "Spain"], OptionsOf(reader, combo));

        var value = Assert.IsType<StringObject>(reader.Resolve(combo["V"]));
        Assert.Equal("Bulgaria", value.Value);

        var da = Assert.IsType<StringObject>(reader.Resolve(combo["DA"]));
        Assert.Contains("Tf", da.Value);
        Assert.Contains("Bulgaria", FormTestSupport.NormalAppearanceText(reader, combo));
    }

    [Fact]
    public void ListBoxSavesChoiceFieldWithoutComboFlag()
    {
        var reader = Reload(WithFields());
        var list = FormTestSupport.Field(reader, "Color");

        Assert.Equal("Ch", FormTestSupport.NameValue(reader, list, "FT"));
        Assert.Equal(0, FlagsOf(reader, list) & ComboFlag);
        Assert.Equal(["Red", "Green", "Blue"], OptionsOf(reader, list));

        var value = Assert.IsType<StringObject>(reader.Resolve(list["V"]));
        Assert.Equal("Green", value.Value);
        Assert.Contains("Green", FormTestSupport.NormalAppearanceText(reader, list));
    }

    [Fact]
    public void UnselectedRadioGroupSavesOffValue()
    {
        var document = BuildDocument();
        var group = RadioGroup();
        group.SelectedValue = null;
        document.FormFields.Add(group);

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Size");
        Assert.Equal("Off", FormTestSupport.NameValue(reader, field, "V"));

        var kids = Assert.IsType<ArrayObject>(reader.Resolve(field["Kids"]));
        foreach (var entry in kids)
        {
            var kid = Assert.IsType<DictionaryObject>(reader.Resolve(entry));
            Assert.Equal("Off", FormTestSupport.NameValue(reader, kid, "AS"));
        }
    }

    [Fact]
    public void FlattenBeforeSaveDrawsSelectionStatically()
    {
        var document = WithFields();
        document.Flatten();

        Assert.Empty(document.FormFields);

        var reader = Reload(document);
        Assert.False(FormTestSupport.Catalog(reader).ContainsKey("AcroForm"));

        var content = AllPageContent(reader);
        Assert.Contains("Bulgaria", content);
        Assert.Contains("Green", content);
        Assert.Contains("c\n", content);
        Assert.Contains("f\n", content);
    }

    [Fact]
    public void FlattenAfterReloadDrawsSelectionAndDropsForm()
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

        var content = AllPaintedContent(reader);
        Assert.Contains("Bulgaria", content);
        Assert.Contains("Green", content);
        Assert.Contains("c\n", content);
        Assert.Contains("f\n", content);
    }

    [Fact]
    public void RadioGroupWithFewerThanTwoOptionsThrows()
    {
        var document = BuildDocument();
        var group = new RadioGroupFieldDefinition("Lonely");
        group.Options.Add(new RadioOptionDefinition("Only") { X = 100, Y = 700, Width = 16, Height = 16 });
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.ToArray);
    }

    [Fact]
    public void RadioGroupWithDuplicateValuesThrows()
    {
        var document = BuildDocument();
        var group = new RadioGroupFieldDefinition("Twins");
        group.Options.Add(new RadioOptionDefinition("Same") { X = 100, Y = 700, Width = 16, Height = 16 });
        group.Options.Add(new RadioOptionDefinition("Same") { X = 100, Y = 670, Width = 16, Height = 16 });
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.ToArray);
    }

    [Fact]
    public void RadioGroupWithUnknownSelectionThrows()
    {
        var document = BuildDocument();
        var group = RadioGroup();
        group.SelectedValue = "ExtraLarge";
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.ToArray);
    }

    [Fact]
    public void RadioOptionValueCannotBeOffOrEmpty()
    {
        Assert.Throws<ArgumentException>(() => new RadioOptionDefinition("Off"));
        Assert.Throws<ArgumentException>(() => new RadioOptionDefinition(string.Empty));
    }
}

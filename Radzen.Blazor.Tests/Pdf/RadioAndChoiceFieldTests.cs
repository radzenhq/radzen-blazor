#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class RadioAndChoiceFieldTests
{
    private static PortableDocument BuildDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Survey", "Helvetica");
        return new DocumentRenderer().Render(document);
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

    private static PortableDocument WithFields()
    {
        var document = BuildDocument();
        document.FormFields.Add(RadioGroup());
        document.FormFields.Add(Combo());
        document.FormFields.Add(ListBox());
        return document;
    }

    [Fact]
    public void RadioGroupSavesParentFieldWithKidWidgets()
    {
        var emission = Emit(WithFields());
        var parent = Line(emission, "/T (Size)");

        Carries("radio parent field", "/FT /Btn", parent);
        CarriesFlag("radio parent field", parent, "Ff", 32768);
        Carries("radio parent field", "/V /Medium", parent);
        Carries("radio parent field", "/DV /Medium", parent);

        var kids = References("radio parent field", "Kids", 3, parent);
        string[] states = ["/AS /Off", "/AS /Medium", "/AS /Off"];

        for (var i = 0; i < kids.Length; i++)
        {
            var kid = IndirectObject(emission, kids[i]);
            Carries($"radio kid {kids[i]} 0 R", "/Subtype /Widget", kid);
            Carries($"radio kid {kids[i]} 0 R", "/Parent ", kid);
            Carries($"radio kid {kids[i]} 0 R", states[i], kid);
        }
    }

    [Fact]
    public void RadioKidWidgetsCarryPerOptionAppearances()
    {
        var emission = Emit(WithFields());
        var kids = References("radio parent field", "Kids", 3, Line(emission, "/T (Size)"));
        string[] values = ["Small", "Medium", "Large"];

        for (var i = 0; i < kids.Length; i++)
        {
            var kid = IndirectObject(emission, kids[i]);
            var subject = $"radio kid {kids[i]} 0 R";
            var appearances = Shaped(
                subject,
                $@"/AP << /N << /{values[i]} (\d+) 0 R /Off (\d+) 0 R >> >>",
                kid);

            var on = IndirectObject(emission, appearances.Groups[1].Value);
            var off = IndirectObject(emission, appearances.Groups[2].Value);

            Carries($"{values[i]} appearance", " c\n", on);
            Carries($"{values[i]} appearance", "\nf\n", on);
            Carries($"{values[i]} Off appearance", "\nS\n", off);
            Lacks($"{values[i]} Off appearance", "\nf\n", off);
        }
    }

    [Fact]
    public void RadioKidWidgetsLandOnTheirPageAnnots()
    {
        var emission = Emit(WithFields());
        var widgets = References("page", "Annots", 5, Line(emission, "/Type /Page "))
            .Select(number => (Number: number, Body: IndirectObject(emission, number)))
            .ToList();

        foreach (var widget in widgets)
        {
            Carries($"page annotation {widget.Number} 0 R", "/Subtype /Widget", widget.Body);
        }

        var parented = widgets.Where(widget => widget.Body.Contains("/Parent ", StringComparison.Ordinal)).ToList();
        Assert.True(
            parented.Count == 3,
            $"Expected 3 of the 5 page widgets to carry '/Parent ', found {parented.Count}."
            + $" Widgets: {string.Join(", ", widgets.Select(widget => widget.Number + " 0 R"))}");

        References("AcroForm", "Fields", 3, Line(emission, "/Fields ["));
    }

    [Fact]
    public void ComboBoxSavesChoiceFieldWithComboFlag()
    {
        var emission = Emit(WithFields());
        var combo = Line(emission, "/T (Country)");

        Carries("combo field", "/FT /Ch", combo);
        CarriesFlag("combo field", combo, "Ff", 131072);
        Carries("combo field", "/Opt [(Bulgaria) (Germany) (Spain)]", combo);
        Carries("combo field", "/V (Bulgaria)", combo);
        Shaped("combo field /DA", @"/DA \([^)]*Tf[^)]*\)", combo);

        var appearance = Shaped("combo field", @"/AP << /N (\d+) 0 R >>", combo);
        Carries("combo appearance", "(Bulgaria)", IndirectObject(emission, appearance.Groups[1].Value));
    }

    [Fact]
    public void ListBoxSavesChoiceFieldWithoutComboFlag()
    {
        var emission = Emit(WithFields());
        var list = Line(emission, "/T (Color)");

        Carries("list field", "/FT /Ch", list);
        LacksFlag("list field", list, "Ff", 131072);
        Carries("list field", "/Opt [(Red) (Green) (Blue)]", list);
        Carries("list field", "/V (Green)", list);

        var appearance = Shaped("list field", @"/AP << /N (\d+) 0 R >>", list);
        Carries("list appearance", "(Green)", IndirectObject(emission, appearance.Groups[1].Value));
    }

    [Fact]
    public void UnselectedRadioGroupSavesOffValue()
    {
        var document = BuildDocument();
        var group = RadioGroup();
        group.SelectedValue = null;
        document.FormFields.Add(group);

        var emission = Emit(document);
        var parent = Line(emission, "/T (Size)");

        Carries("radio parent field", "/V /Off", parent);

        foreach (var number in References("radio parent field", "Kids", 3, parent))
        {
            Carries($"radio kid {number} 0 R", "/AS /Off", IndirectObject(emission, number));
        }
    }

    [Fact]
    public void FlattenBeforeSaveDrawsSelectionStatically()
    {
        var document = WithFields();
        document.Flatten();

        Assert.Empty(document.FormFields);

        var emission = Emit(document);
        Lacks("catalog", "/AcroForm", Line(emission, "/Type /Catalog"));

        var painted = FlattenedContent(emission);
        Carries("flattened content", "(Bulgaria) Tj", painted);
        Carries("flattened content", "(Green) Tj", painted);
        Carries("flattened content", " c\n", painted);
        Carries("flattened content", "\nf\n", painted);
    }

    [Fact]
    public void FlattenBeforeSaveDrawsEveryRadioOutlineAndOnlyTheSelectedDot()
    {
        var document = BuildDocument();
        document.FormFields.Add(RadioGroup());

        document.Flatten();

        var painted = FlattenedContent(Emit(document));
        var lines = painted.Split('\n');
        var strokes = lines.Count(line => line == "S");
        var fills = lines.Count(line => line == "f");

        Assert.True(
            strokes == 3,
            $"Expected 3 lone 'S' strokes in the flattened content, found {strokes}.\n{Excerpt(painted)}");
        Assert.True(
            fills == 1,
            $"Expected 1 lone 'f' fill in the flattened content, found {fills}.\n{Excerpt(painted)}");
    }

    private static string FlattenedContent(string emission)
    {
        var contents = References("page", "Contents", 2, Line(emission, "/Type /Page "));
        return IndirectObject(emission, contents[1]);
    }

    [Fact]
    public void FlattenAfterReloadDrawsSelectionAndDropsForm()
    {
        using var stream = new MemoryStream(WithFields().ToArray());
        var reloaded = PortableDocument.LoadFromStream(stream);
        reloaded.Flatten();

        Assert.Null(reloaded.AcroForm);

        var reader = DocumentReader.Parse(reloaded.ToArray());
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

    private static string AllPaintedContent(DocumentReader reader)
    {
        var page = FormTestSupport.FirstPage(reader);
        Assert.True(page.TryGetValue("Contents", out var contents), "page has no /Contents");
        var resolved = reader.Resolve(contents!);
        var text = new StringBuilder();

        if (resolved is StreamObject single)
        {
            text.Append(FormTestSupport.Decode(single));
        }
        else
        {
            foreach (var part in (ArrayObject)resolved)
            {
                text.Append(FormTestSupport.Decode((StreamObject)reader.Resolve(part)));
            }
        }

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

    [Fact]
    public void FlattenRadioGroupWithFewerThanTwoOptionsThrows()
    {
        var document = BuildDocument();
        var group = new RadioGroupFieldDefinition("Lonely");
        group.Options.Add(new RadioOptionDefinition("Only") { X = 100, Y = 700, Width = 16, Height = 16 });
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.Flatten);
    }

    [Fact]
    public void FlattenRadioGroupWithDuplicateValuesThrows()
    {
        var document = BuildDocument();
        var group = new RadioGroupFieldDefinition("Twins");
        group.Options.Add(new RadioOptionDefinition("Same") { X = 100, Y = 700, Width = 16, Height = 16 });
        group.Options.Add(new RadioOptionDefinition("Same") { X = 100, Y = 670, Width = 16, Height = 16 });
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.Flatten);
    }

    [Fact]
    public void FlattenRadioGroupWithUnknownSelectionThrows()
    {
        var document = BuildDocument();
        var group = RadioGroup();
        group.SelectedValue = "ExtraLarge";
        document.FormFields.Add(group);

        Assert.Throws<InvalidOperationException>(document.Flatten);
    }
}

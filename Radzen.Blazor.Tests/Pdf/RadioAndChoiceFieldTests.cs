#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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

    private static string Emit(PortableDocument document) => Encoding.Latin1.GetString(document.ToArray());

    private static string Excerpt(string text)
        => text.Length <= 600 ? text : text[..600] + "...";

    private static string Line(string emission, string marker)
    {
        var index = emission.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(
            index >= 0,
            $"No emitted line carries '{marker}'. Emission starts:\n{Excerpt(emission)}");

        var start = emission.LastIndexOf('\n', index) + 1;
        var end = emission.IndexOf('\n', index);
        return end < 0 ? emission[start..] : emission[start..end];
    }

    private static string IndirectObject(string emission, string number)
    {
        var header = $"\n{number} 0 obj\n";
        var start = emission.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Object '{number} 0 obj' is not in the emission.");

        var body = start + header.Length;
        var end = emission.IndexOf("\nendobj", body, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Object '{number} 0 obj' has no endobj. Body starts:\n{Excerpt(emission[body..])}");
        return emission[body..end];
    }

    private static void Carries(string subject, string fragment, string container)
        => Assert.True(
            container.Contains(fragment, StringComparison.Ordinal),
            $"{subject} is missing '{fragment}'.\n{subject}:\n{Excerpt(container)}");

    private static void Lacks(string subject, string fragment, string container)
        => Assert.True(
            !container.Contains(fragment, StringComparison.Ordinal),
            $"{subject} unexpectedly carries '{fragment}'.\n{subject}:\n{Excerpt(container)}");

    private static Match Shaped(string subject, string pattern, string container)
    {
        var match = Regex.Match(container, pattern);
        Assert.True(match.Success, $"{subject} does not match '{pattern}'.\n{subject}:\n{Excerpt(container)}");
        return match;
    }

    private static string[] References(string subject, string key, int count, string container)
    {
        var pattern = $"/{key} \\[{string.Join(" ", Enumerable.Repeat(@"(\d+) 0 R", count))}\\]";
        var match = Shaped($"{subject} /{key} with {count} references", pattern, container);
        return [.. match.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Select(group => group.Value)];
    }

    [Fact]
    public void RadioGroupSavesParentFieldWithKidWidgets()
    {
        var emission = Emit(WithFields());
        var parent = Line(emission, "/T (Size)");

        Carries("radio parent field", "/FT /Btn", parent);
        Carries("radio parent field", "/Ff 32768", parent);
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
        Carries("combo field", "/Ff 131072", combo);
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
        Lacks("list field", "/Ff 131072", list);
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

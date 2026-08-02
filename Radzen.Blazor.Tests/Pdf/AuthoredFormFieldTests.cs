#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AuthoredFormFieldTests
{
    private static DocumentRenderer Accessible() => new() { Accessibility = PdfUaConformance.PdfUa1 };

    private static Document Accessible(out Paragraph paragraph)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;

        paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static Document Plain(out Paragraph paragraph)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static List<DictionaryObject> Widgets(DocumentReader reader, int pageIndex)
    {
        var page = BuildTestSupport.PageLeaves(reader)[pageIndex].Page;
        var widgets = new List<DictionaryObject>();
        if (page.TryGetValue("Annots", out var annots) && reader.Resolve(annots!) is ArrayObject array)
        {
            foreach (var item in array)
            {
                var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(item));
                if (BuildTestSupport.Name(reader, annotation, "Subtype") == "Widget")
                {
                    widgets.Add(annotation);
                }
            }
        }

        return widgets;
    }

    private static ArrayObject FormFields(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        var form = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["AcroForm"]));
        return Assert.IsType<ArrayObject>(reader.Resolve(form["Fields"]));
    }

    private static double[] Rect(DocumentReader reader, DictionaryObject widget)
    {
        var array = Assert.IsType<ArrayObject>(reader.Resolve(widget["Rect"]));
        return [.. array.Select(value => ((NumberObject)reader.Resolve(value)).DoubleValue)];
    }

    private static string Text(DocumentReader reader, DictionaryObject dictionary, string key)
        => Assert.IsType<StringObject>(reader.Resolve(dictionary[key])).Value;

    private static string PageContent(DocumentReader reader, int pageIndex)
    {
        var page = BuildTestSupport.PageLeaves(reader)[pageIndex].Page;
        var resolved = reader.Resolve(page["Contents"]);
        if (resolved is StreamObject stream)
        {
            return FormTestSupport.Decode(stream);
        }

        var text = new System.Text.StringBuilder();
        foreach (var part in (ArrayObject)resolved)
        {
            text.Append(FormTestSupport.Decode((StreamObject)reader.Resolve(part)));
        }

        return text.ToString();
    }

    [Fact]
    public void TextInput_IsAWidgetListedInTheAcroFormWithAGeneratedAppearance()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add("Name: ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Full name" });

        var reader = BuildTestSupport.Read(document);
        var widget = Assert.Single(Widgets(reader, 0));

        Assert.Equal("Tx", BuildTestSupport.Name(reader, widget, "FT"));
        Assert.Equal("name", Text(reader, widget, "T"));
        Assert.Equal("Ada", Text(reader, widget, "V"));
        Assert.Equal("Full name", Text(reader, widget, "TU"));

        var appearance = Assert.IsType<DictionaryObject>(reader.Resolve(widget["AP"]));
        Assert.IsType<StreamObject>(reader.Resolve(appearance["N"]));

        var fields = FormFields(reader);
        Assert.Same(widget, Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(fields))));
    }

    [Fact]
    public void CheckBox_IsAButtonWidgetWithOnAndOffAppearances()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new CheckBox("agree") { Checked = true, Required = true });

        var reader = BuildTestSupport.Read(document);
        var widget = Assert.Single(Widgets(reader, 0));

        Assert.Equal("Btn", BuildTestSupport.Name(reader, widget, "FT"));
        Assert.Equal("Yes", BuildTestSupport.Name(reader, widget, "V"));
        Assert.Equal("Yes", BuildTestSupport.Name(reader, widget, "AS"));
        Assert.Equal(2, BuildTestSupport.Int(widget, "Ff"));

        var states = Assert.IsType<DictionaryObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(widget["AP"]))["N"]));
        Assert.IsType<StreamObject>(reader.Resolve(states["Yes"]));
        Assert.IsType<StreamObject>(reader.Resolve(states["Off"]));
    }

    [Fact]
    public void DropDown_IsAChoiceWidgetCarryingItsOptions()
    {
        var document = Plain(out var paragraph);
        var drop = new DropDown("country") { Value = "Bulgaria" };
        drop.Options.Add("Bulgaria");
        drop.Options.Add("Germany");
        paragraph.Inlines.Add(drop);

        var reader = BuildTestSupport.Read(document);
        var widget = Assert.Single(Widgets(reader, 0));

        Assert.Equal("Ch", BuildTestSupport.Name(reader, widget, "FT"));
        Assert.Equal("Bulgaria", Text(reader, widget, "V"));

        var options = Assert.IsType<ArrayObject>(reader.Resolve(widget["Opt"]));
        Assert.Equal(
            new[] { "Bulgaria", "Germany" },
            options.Select(option => ((StringObject)reader.Resolve(option)).Value).ToArray());
    }

    [Fact]
    public void WidgetGeometry_ComesFromTheLineLayoutAndNotFromTheCaller()
    {
        var document = Plain(out var paragraph);
        paragraph.Font.Size = 10;
        paragraph.Inlines.Add("Name: ").Font.Size = 10;
        paragraph.Inlines.Add(new TextInput("name") { Width = 90 });

        var reader = BuildTestSupport.Read(document);
        var rect = Rect(reader, Assert.Single(Widgets(reader, 0)));

        Assert.Equal(90, rect[2] - rect[0], 3);
        Assert.Equal(14, rect[3] - rect[1], 3);
        Assert.True(rect[0] > 72, "the widget starts after the text that precedes it, not at the caller's origin");
    }

    [Fact]
    public void FieldInAParagraph_FlowsWithTheTextOntoTheNextPage()
    {
        var document = Plain(out var paragraph);
        document.Sections[0].PageSize = new PageSize(400, 220);

        paragraph.Inlines.Add(string.Join(' ', Enumerable.Repeat("wrapping words that fill the page", 12)));
        paragraph.Inlines.Add(" sign here ");
        paragraph.Inlines.Add(new CheckBox("signed"));

        var reader = BuildTestSupport.Read(document);

        Assert.Equal(2, BuildTestSupport.PageLeaves(reader).Count);
        Assert.Empty(Widgets(reader, 0));
        Assert.Single(Widgets(reader, 1));
    }

    [Fact]
    public void RadioGroup_IsOneFieldWithSeveralWidgetsSharingItsNameAndDistinctExportValues()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S"));
        paragraph.Inlines.Add(" small ");
        paragraph.Inlines.Add(new RadioButton("size", "M") { Selected = true });
        paragraph.Inlines.Add(" medium");

        var reader = BuildTestSupport.Read(document);
        var widgets = Widgets(reader, 0);

        Assert.Equal(2, widgets.Count);

        var group = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(FormFields(reader))));
        Assert.Equal("size", Text(reader, group, "T"));
        Assert.Equal("M", BuildTestSupport.Name(reader, group, "V"));
        Assert.Equal(1 << 15, BuildTestSupport.Int(group, "Ff"));

        var kids = Assert.IsType<ArrayObject>(reader.Resolve(group["Kids"]));
        Assert.Equal(2, kids.Count);

        Assert.Equal(
            new[] { "Off", "M" },
            widgets.Select(widget => BuildTestSupport.Name(reader, widget, "AS")).ToArray());

        foreach (var widget in widgets)
        {
            Assert.Same(group, reader.Resolve(widget["Parent"]));
            Assert.False(widget.ContainsKey("T"), "a radio button widget takes its name from the group");
        }

        var states = widgets.Select(widget => Assert.IsType<DictionaryObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(widget["AP"]))["N"])));
        Assert.Equal(
            new[] { "S", "M" },
            states.Select(state => state.Keys.Single(key => key != "Off")).ToArray());
    }

    [Fact]
    public void RadioGroup_WithTwoButtonsSelected_Fails()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S") { Selected = true });
        paragraph.Inlines.Add(new RadioButton("size", "M") { Selected = true });

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("only one button of a group may be selected", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoFieldsSharingAName_Fails()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new TextInput("name"));
        paragraph.Inlines.Add(new TextInput("name"));

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("Two form fields are named 'name'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TaggedRender_WrapsEachWidgetInAFormElementJoinedByObjr()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Name: ");
        paragraph.Inlines.Add(new TextInput("name") { Label = "Full name" });
        paragraph.Inlines.Add(" please");

        var reader = BuildTestSupport.Read(document, Accessible());
        var structRoot = TaggedStructureProbe.StructRoot(reader);
        var form = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "Form");

        Assert.Equal("Full name", Text(reader, form.Dict, "Alt"));

        var objr = Assert.Single(form.ObjectReferences);
        var widget = Assert.IsType<DictionaryObject>(reader.Resolve(objr["Obj"]!));
        Assert.Same(Assert.Single(Widgets(reader, 0)), widget);

        var structParent = Assert.IsType<NumberObject>(reader.Resolve(widget["StructParent"])).IntValue;
        Assert.Same(
            form.Dict,
            Assert.IsType<DictionaryObject>(TaggedStructureProbe.ParentTreeEntry(reader, structRoot, structParent)));
    }

    [Fact]
    public void TaggedRender_KeepsTheFormElementInReadingOrderInsideItsParagraph()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Name: ");
        paragraph.Inlines.Add(new TextInput("name") { Label = "Full name" });
        paragraph.Inlines.Add(" please");

        var reader = BuildTestSupport.Read(document, Accessible());
        var paragraphElement = TaggedStructureProbe.Single(TaggedStructureProbe.Root(reader), "P");

        Assert.Equal(3, paragraphElement.Kids.Count);
        Assert.IsType<int>(paragraphElement.Kids[0]);
        Assert.IsType<ProbeElement>(paragraphElement.Kids[1]);
        Assert.IsType<int>(paragraphElement.Kids[2]);
    }

    [Fact]
    public void TaggedRender_WithAFormField_SavesAsPdfUa()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Agree ");
        paragraph.Inlines.Add(new CheckBox("agree") { Label = "I agree" });

        var bytes = Accessible().ToArray(document);

        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void UntaggedRender_EmitsTheWidgetWithoutAnyStructure()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new CheckBox("agree"));

        var reader = BuildTestSupport.Read(document);
        var widget = Assert.Single(Widgets(reader, 0));

        Assert.False(widget.ContainsKey("StructParent"), "an untagged widget carries no /StructParent");
        Assert.False(ContentTestHelpers.Catalog(reader).ContainsKey("StructTreeRoot"), "no structure tree is written");
    }

    [Fact]
    public void PdfUaDocument_GivenACallerSuppliedFormFieldDefinition_FailsAtSave()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Agree ");
        paragraph.Inlines.Add(new CheckBox("agree") { Label = "I agree" });

        var rendered = Accessible().Render(document);
        rendered.FormFields.Add(new TextFieldDefinition("stamped") { Width = 60, Height = 12 });

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);

        Assert.Contains(
            "PDF/UA requires every annotation to be referenced from the structure tree",
            error.Message,
            StringComparison.Ordinal);
        Assert.Contains("author the field in the document instead", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUaDocument_GivenAnUntaggedAnnotation_FailsAtSave()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Agree ");
        paragraph.Inlines.Add(new CheckBox("agree") { Label = "I agree" });

        var rendered = Accessible().Render(document);
        rendered.Pages[0].Annotations.Add(new SquareAnnotation(new PdfRect(10, 10, 40, 40)));

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);

        Assert.Contains("an annotation was added to Page.Annotations", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUaDocument_GivenACallerSuppliedFormFieldDefinition_FailsIncrementalSaveToo()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Agree ");
        paragraph.Inlines.Add(new CheckBox("agree") { Label = "I agree" });

        using var buffer = new MemoryStream(Accessible().ToArray(document));
        var loaded = PortableDocument.LoadFromStream(buffer);
        loaded.Accessibility = PdfUaConformance.PdfUa1;
        loaded.FormFields.Add(new TextFieldDefinition("stamped") { Width = 60, Height = 12 });

        using var output = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => loaded.SaveIncremental(output));

        Assert.Contains("author the field in the document instead", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void Flatten_BakesTheAuthoredFieldAppearanceAndDropsTheWidget()
    {
        var document = Plain(out var paragraph);
        paragraph.Font.Family = null;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });
        paragraph.Inlines.Add(new CheckBox("agree") { Checked = true });

        var rendered = new DocumentRenderer().Render(document);
        rendered.Flatten();

        var reader = DocumentReader.Parse(rendered.ToArray());

        Assert.Empty(Widgets(reader, 0));
        Assert.False(ContentTestHelpers.Catalog(reader).ContainsKey("AcroForm"), "the form is gone");
        Assert.Contains("(Ada) Tj", PageContent(reader, 0), StringComparison.Ordinal);
    }

    [Fact]
    public void RadioGroupExportingOff_NamesItsAppearanceStatesByOptionIndex()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("stance", "On"));
        paragraph.Inlines.Add(" on ");
        paragraph.Inlines.Add(new RadioButton("stance", "Off") { Selected = true });
        paragraph.Inlines.Add(" off");

        var reader = BuildTestSupport.Read(document);
        var widgets = Widgets(reader, 0);
        var group = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(FormFields(reader))));

        Assert.Equal(
            new[] { "On", "Off" },
            Assert.IsType<ArrayObject>(reader.Resolve(group["Opt"]))
                .Select(value => Assert.IsType<StringObject>(reader.Resolve(value)).Value)
                .ToArray());
        Assert.Equal("1", BuildTestSupport.Name(reader, group, "V"));
        Assert.Equal(
            new[] { "Off", "1" },
            widgets.Select(widget => BuildTestSupport.Name(reader, widget, "AS")).ToArray());

        var states = widgets.Select(widget => Assert.IsType<DictionaryObject>(
            reader.Resolve(Assert.IsType<DictionaryObject>(reader.Resolve(widget["AP"]))["N"])));
        Assert.Equal(
            new[] { "0", "1" },
            states.Select(state => state.Keys.Single(key => key != "Off")).ToArray());
    }

    [Fact]
    public void RadioGroupWithoutAnOffExport_CarriesNoOptArray()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S"));
        paragraph.Inlines.Add(new RadioButton("size", "M"));

        var reader = BuildTestSupport.Read(document);
        var group = Assert.IsType<DictionaryObject>(reader.Resolve(Assert.Single(FormFields(reader))));

        Assert.False(group.ContainsKey("Opt"), "an unambiguous group keeps its export values as state names");
    }

    [Fact]
    public void RadioGroup_WithTwoButtonsExportingOneValue_Fails()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S"));
        paragraph.Inlines.Add(new RadioButton("size", "S"));

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("give each button of a group a distinct Value", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RadioGroupRules_AreEnforcedByLayoutBeforeAnyRendererRuns()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S") { Selected = true });
        paragraph.Inlines.Add(new RadioButton("size", "M") { Selected = true });

        var error = Assert.Throws<InvalidOperationException>(
            () => Radzen.Documents.Layout.DocumentLayouter.Layout(document));

        Assert.Contains("only one button of a group may be selected", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LinkedFormField_EmitsALinkAnnotationOverTheWidgetRect()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Link = "https://www.radzen.com/" });

        var reader = BuildTestSupport.Read(document);
        var widget = Assert.Single(Widgets(reader, 0));
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        var annotations = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        var link = Assert.Single(annotations
            .Select(item => Assert.IsType<DictionaryObject>(reader.Resolve(item)))
            .Where(annotation => BuildTestSupport.Name(reader, annotation, "Subtype") == "Link"));

        var widgetRect = Rect(reader, widget);
        var linkRect = Rect(reader, link);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(widgetRect[i], linkRect[i], 3);
        }
    }
}

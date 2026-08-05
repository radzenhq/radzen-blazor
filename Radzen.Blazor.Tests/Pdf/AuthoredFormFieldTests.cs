#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

        paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static Document Plain(out Paragraph paragraph)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        paragraph = document.Sections.Add().Blocks.Add(new Paragraph());
        paragraph.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static string[] PageNumbers(string emission)
        => [.. Regex.Matches(Line(emission, "/Type /Pages"), @"(\d+) 0 R").Select(match => match.Groups[1].Value)];

    private static string PageObject(string emission, int index)
    {
        var pages = PageNumbers(emission);
        Assert.True(index < pages.Length, $"The page tree has {pages.Length} kids; page {index} was requested.");
        return IndirectObject(emission, pages[index]);
    }

    private static List<(string Number, string Body)> PageWidgets(string emission, int pageIndex)
    {
        var widgets = new List<(string Number, string Body)>();
        var annots = Regex.Match(PageObject(emission, pageIndex), @"/Annots \[([^\]]*)\]");
        if (!annots.Success)
        {
            return widgets;
        }

        foreach (Match reference in Regex.Matches(annots.Groups[1].Value, @"(\d+) 0 R"))
        {
            var number = reference.Groups[1].Value;
            var body = IndirectObject(emission, number);
            if (body.Contains("/Subtype /Widget", StringComparison.Ordinal))
            {
                widgets.Add((number, body));
            }
        }

        return widgets;
    }

    private static string[] AcroFormFields(string emission, int count)
        => References("AcroForm", "Fields", count, Line(emission, "/Fields ["));

    private static double[] Rect(string subject, string body)
    {
        var match = Shaped(subject, @"/Rect \[(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+)\]", body);
        return [.. Enumerable.Range(1, 4).Select(
            index => double.Parse(match.Groups[index].Value, CultureInfo.InvariantCulture))];
    }

    private static (string Number, string Body) SoleStructureElement(string emission, string type)
    {
        var matches = Regex.Matches(emission, $@"\n(\d+) 0 obj\n(<< /Type /StructElem /S /{type} [^\n]*)\n");
        Assert.True(
            matches.Count == 1,
            $"Expected 1 '/S /{type}' structure element, found {matches.Count}.");
        return (matches[0].Groups[1].Value, matches[0].Groups[2].Value);
    }

    [Fact]
    public void TextInput_IsAWidgetListedInTheAcroFormWithAGeneratedAppearance()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add("Name: ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Full name" });

        var emission = Emit(document);
        var widget = Assert.Single(PageWidgets(emission, 0));

        Carries("text widget", "/FT /Tx", widget.Body);
        Carries("text widget", "/T (name)", widget.Body);
        Carries("text widget", "/V (Ada)", widget.Body);
        Carries("text widget", "/TU (Full name)", widget.Body);

        var appearance = Shaped("text widget", @"/AP << /N (\d+) 0 R >>", widget.Body);
        Carries("text appearance", ">>\nstream\n", IndirectObject(emission, appearance.Groups[1].Value));

        Assert.Equal(widget.Number, Assert.Single(AcroFormFields(emission, 1)));
    }

    [Fact]
    public void CheckBox_IsAButtonWidgetWithOnAndOffAppearances()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new CheckBox("agree") { Checked = true, Required = true });

        var emission = Emit(document);
        var widget = Assert.Single(PageWidgets(emission, 0));

        Carries("check box widget", "/FT /Btn", widget.Body);
        Carries("check box widget", "/V /Yes", widget.Body);
        Carries("check box widget", "/AS /Yes", widget.Body);
        CarriesFlag("check box widget", widget.Body, "Ff", 2);

        var states = Shaped("check box widget", @"/AP << /N << /Yes (\d+) 0 R /Off (\d+) 0 R >> >>", widget.Body);
        Carries("check box on appearance", ">>\nstream\n", IndirectObject(emission, states.Groups[1].Value));
        Carries("check box off appearance", ">>\nstream\n", IndirectObject(emission, states.Groups[2].Value));
    }

    [Fact]
    public void DropDown_IsAChoiceWidgetCarryingItsOptions()
    {
        var document = Plain(out var paragraph);
        var drop = new DropDown("country") { Value = "Bulgaria" };
        drop.Options.Add("Bulgaria");
        drop.Options.Add("Germany");
        paragraph.Inlines.Add(drop);

        var widget = Assert.Single(PageWidgets(Emit(document), 0));

        Carries("choice widget", "/FT /Ch", widget.Body);
        Carries("choice widget", "/V (Bulgaria)", widget.Body);
        Carries("choice widget", "/Opt [(Bulgaria) (Germany)]", widget.Body);
    }

    [Fact]
    public void WidgetGeometry_ComesFromTheLineLayoutAndNotFromTheCaller()
    {
        var document = Plain(out var paragraph);
        paragraph.Font.Size = 10;
        paragraph.Inlines.Add("Name: ").Font.Size = 10;
        paragraph.Inlines.Add(new TextInput("name") { Width = 90 });

        var emission = Emit(document);
        var rect = Rect("text widget", Assert.Single(PageWidgets(emission, 0)).Body);

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

        var emission = Emit(document);

        Assert.Equal(2, PageNumbers(emission).Length);
        Assert.Empty(PageWidgets(emission, 0));
        Assert.Single(PageWidgets(emission, 1));
    }

    [Fact]
    public void RadioGroup_IsOneFieldWithSeveralWidgetsSharingItsNameAndDistinctExportValues()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S"));
        paragraph.Inlines.Add(" small ");
        paragraph.Inlines.Add(new RadioButton("size", "M") { Selected = true });
        paragraph.Inlines.Add(" medium");

        var emission = Emit(document);
        var widgets = PageWidgets(emission, 0);

        Assert.Equal(2, widgets.Count);

        var number = Assert.Single(AcroFormFields(emission, 1));
        var group = IndirectObject(emission, number);

        Carries("radio group", "/T (size)", group);
        Carries("radio group", "/V /M", group);
        CarriesFlag("radio group", group, "Ff", 1 << 15);

        var kids = References("radio group", "Kids", 2, group);
        string[] states = ["/AS /Off", "/AS /M"];
        string[] on = ["S", "M"];

        for (var i = 0; i < kids.Length; i++)
        {
            var kid = IndirectObject(emission, kids[i]);
            var subject = $"radio kid {kids[i]} 0 R";

            Assert.Equal(widgets[i].Number, kids[i]);
            Carries(subject, $"/Parent {number} 0 R", kid);
            Lacks(subject, "/T (", kid);
            Carries(subject, states[i], kid);
            Shaped(subject, $@"/AP << /N << /{on[i]} \d+ 0 R /Off \d+ 0 R >> >>", kid);
        }
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

        var emission = Emit(document, Accessible());
        var form = SoleStructureElement(emission, "Form");

        Carries("form element", "/Alt (Full name)", form.Body);

        var objr = Shaped("form element", @"/K \[<< /Type /OBJR /Pg \d+ 0 R /Obj (\d+) 0 R >>\]", form.Body);
        var widget = Assert.Single(PageWidgets(emission, 0));

        Assert.Equal(widget.Number, objr.Groups[1].Value);

        var parentTree = Shaped(
            "structure tree root",
            @"/ParentTree (\d+) 0 R",
            Line(emission, "/Type /StructTreeRoot"));

        Shaped(
            "parent tree",
            $@"[\[ ]{NumberIn(widget.Body, "StructParent")} {form.Number} 0 R",
            IndirectObject(emission, parentTree.Groups[1].Value));
    }

    [Fact]
    public void TaggedRender_KeepsTheFormElementInReadingOrderInsideItsParagraph()
    {
        var document = Accessible(out var paragraph);
        paragraph.Inlines.Add("Name: ");
        paragraph.Inlines.Add(new TextInput("name") { Label = "Full name" });
        paragraph.Inlines.Add(" please");

        var emission = Emit(document, Accessible());
        var kids = Shaped(
            "paragraph element",
            @"/K \[\d+ (\d+) 0 R \d+\]",
            SoleStructureElement(emission, "P").Body);

        Carries("paragraph kid", "/Type /StructElem", IndirectObject(emission, kids.Groups[1].Value));
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

        var emission = Emit(document);
        var widget = Assert.Single(PageWidgets(emission, 0));

        Lacks("check box widget", "/StructParent", widget.Body);
        Lacks("catalog", "/StructTreeRoot", Line(emission, "/Type /Catalog"));
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

        var emission = Emit(rendered);

        Assert.Empty(PageWidgets(emission, 0));
        Lacks("catalog", "/AcroForm", Line(emission, "/Type /Catalog"));

        var contents = References("page", "Contents", 2, PageObject(emission, 0));
        Carries("flattened content", "(Ada) Tj", IndirectObject(emission, contents[1]));
    }

    [Fact]
    public void RadioGroupExportingOff_NamesItsAppearanceStatesByOptionIndex()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("stance", "On"));
        paragraph.Inlines.Add(" on ");
        paragraph.Inlines.Add(new RadioButton("stance", "Off") { Selected = true });
        paragraph.Inlines.Add(" off");

        var emission = Emit(document);
        var group = IndirectObject(emission, Assert.Single(AcroFormFields(emission, 1)));

        Carries("radio group", "/Opt [(On) (Off)]", group);
        Carries("radio group", "/V /1", group);

        var kids = References("radio group", "Kids", 2, group);
        string[] states = ["/AS /Off", "/AS /1"];
        string[] on = ["0", "1"];

        for (var i = 0; i < kids.Length; i++)
        {
            var kid = IndirectObject(emission, kids[i]);
            var subject = $"radio kid {kids[i]} 0 R";

            Carries(subject, states[i], kid);
            Shaped(subject, $@"/AP << /N << /{on[i]} \d+ 0 R /Off \d+ 0 R >> >>", kid);
        }
    }

    [Fact]
    public void RadioGroupWithoutAnOffExport_CarriesNoOptArray()
    {
        var document = Plain(out var paragraph);
        paragraph.Inlines.Add(new RadioButton("size", "S"));
        paragraph.Inlines.Add(new RadioButton("size", "M"));

        var emission = Emit(document);

        Lacks(
            "radio group",
            "/Opt",
            IndirectObject(emission, Assert.Single(AcroFormFields(emission, 1))));
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

        var emission = Emit(document);
        var widget = Assert.Single(PageWidgets(emission, 0));

        var links = new List<string>();
        foreach (var reference in ReferencesIn("page", "Annots", PageObject(emission, 0)))
        {
            var body = IndirectObject(emission, reference);
            if (body.Contains("/Subtype /Link", StringComparison.Ordinal))
            {
                links.Add(body);
            }
        }

        var widgetRect = Rect("text widget", widget.Body);
        var linkRect = Rect("link annotation", Assert.Single(links));
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(widgetRect[i], linkRect[i], 3);
        }
    }
}

#nullable enable
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// FormWriter no longer type-tests FormFieldDefinition: each field kind routes its own
// flattened appearance, text-appearance metadata and created-widget emission through the
// polymorphic members, so a new field type is a new subclass rather than edits across
// the writer. These assert the dispatch each subtype implements.
public class FormFieldDefinitionDispatchTests
{
    private static Page NewPage() => new(Unit.FromPoint(200), Unit.FromPoint(200));

    [Fact]
    public void TextAppearance_IsPresentOnlyForTextBearingFields()
    {
        Assert.NotNull(new TextFieldDefinition("t") { Value = "hi" }.TextAppearance);
        Assert.NotNull(new ChoiceFieldDefinition("c") { Value = "x" }.TextAppearance);
        Assert.Null(new CheckBoxFieldDefinition("b").TextAppearance);
        Assert.Null(new RadioGroupFieldDefinition("r").TextAppearance);
    }

    [Fact]
    public void WriteFlattenedContent_TextField_AddsTextContent()
    {
        var page = NewPage();
        new TextFieldDefinition("t") { Value = "hi", Width = 80, Height = 20 }.WriteFlattenedContent(page);
        Assert.IsType<TextContent>(Assert.Single(page.Content));
    }

    [Fact]
    public void WriteFlattenedContent_EmptyTextField_AddsNothing()
    {
        var page = NewPage();
        new TextFieldDefinition("t").WriteFlattenedContent(page);
        Assert.Empty(page.Content);
    }

    [Fact]
    public void WriteFlattenedContent_CheckBox_DrawsOnlyWhenChecked()
    {
        var unchecked_ = NewPage();
        new CheckBoxFieldDefinition("b") { Width = 12, Height = 12 }.WriteFlattenedContent(unchecked_);
        Assert.Empty(unchecked_.Content);

        var checked_ = NewPage();
        new CheckBoxFieldDefinition("b") { Checked = true, Width = 12, Height = 12 }.WriteFlattenedContent(checked_);
        Assert.IsType<PathContent>(Assert.Single(checked_.Content));
    }

    [Fact]
    public void EmitCreatedField_CheckBox_EmitsBtnWidget()
    {
        var widget = SingleEmittedField(new CheckBoxFieldDefinition("b") { Checked = true, Width = 12, Height = 12 });
        Assert.Equal("Btn", ((NameObject)widget["FT"]!).Value);
        Assert.Equal("Widget", ((NameObject)widget["Subtype"]!).Value);
    }

    [Fact]
    public void EmitCreatedField_RadioGroup_EmitsParentWithKids()
    {
        var radio = new RadioGroupFieldDefinition("r") { SelectedValue = "a" };
        radio.Options.Add(new RadioOptionDefinition("a") { Width = 12, Height = 12 });
        radio.Options.Add(new RadioOptionDefinition("b") { X = 20, Width = 12, Height = 12 });

        var parent = SingleEmittedField(radio);
        Assert.Equal("Btn", ((NameObject)parent["FT"]!).Value);
        Assert.False(parent.ContainsKey("Subtype")); // a parent group field, not a widget itself
        Assert.Equal(2, ((ArrayObject)parent["Kids"]!).Count);
    }

    // Runs the created-field emission and returns the object added to the AcroForm fields list.
    private static DictionaryObject SingleEmittedField(FormFieldDefinition definition)
    {
        using var stream = new MemoryStream();
        var writer = new DocumentWriter(stream);
        var page = writer.Add(new DictionaryObject());
        var fields = new List<DocumentObject>();
        var created = new List<(int, ReferenceObject)>();

        definition.EmitCreatedField(writer, page, fields, created);

        var reference = Assert.IsType<ReferenceObject>(Assert.Single(fields));
        return Assert.IsType<DictionaryObject>(writer.Resolve(reference));
    }
}

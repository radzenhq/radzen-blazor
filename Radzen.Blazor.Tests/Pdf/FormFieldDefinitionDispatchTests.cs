#nullable enable
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Write;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FormFieldDefinitionDispatchTests
{
    [Fact]
    public void WriteFlattenedContent_TextField_AddsTextContent()
    {
        var page = Flatten(new TextFieldDefinition("t") { Value = "hi", Width = 80, Height = 20 });
        Assert.IsType<TextContent>(Assert.Single(page.Content));
    }

    [Fact]
    public void WriteFlattenedContent_EmptyTextField_AddsNothing()
    {
        var page = Flatten(new TextFieldDefinition("t"));
        Assert.Empty(page.Content);
    }

    [Fact]
    public void WriteFlattenedContent_CheckBox_DrawsOnlyWhenChecked()
    {
        var unchecked_ = Flatten(new CheckBoxFieldDefinition("b") { Width = 12, Height = 12 });
        Assert.Empty(unchecked_.Content);

        var checked_ = Flatten(new CheckBoxFieldDefinition("b") { Checked = true, Width = 12, Height = 12 });
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
        Assert.False(parent.ContainsKey("Subtype"));
        Assert.Equal(2, ((ArrayObject)parent["Kids"]!).Count);
    }

    private static DictionaryObject SingleEmittedField(FormFieldDefinition definition)
    {
        using var stream = new MemoryStream();
        var writer = new DocumentWriter(stream);
        var page = writer.Add(new DictionaryObject());
        var fields = new List<DocumentObject>();
        var created = new List<(int, ReferenceObject)>();

        FormFieldEmitter.Emit(
            definition,
            new FormEmitContext(writer, page, fields, created, new FormAppearanceService(new PortableDocument())));

        var reference = Assert.IsType<ReferenceObject>(Assert.Single(fields));
        return Assert.IsType<DictionaryObject>(writer.Resolve(reference));
    }

    private static Page Flatten(FormFieldDefinition definition)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add(PageSizes.A4);
        document.FormFields.Add(definition);
        new FormFlattener(document).Flatten();
        return page;
    }
}

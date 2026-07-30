using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal static class FieldPageValidation
{
    public static void Validate(FormFieldDefinition definition, int pageCount)
    {
        if (definition.PageIndex < 0 || definition.PageIndex >= pageCount)
        {
            throw new InvalidOperationException(
                $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {pageCount} pages.");
        }
    }
}

internal static class RadioGroupValidation
{
    public static void Validate(RadioGroupFieldDefinition radio)
    {
        if (radio.Options.Count < 2)
        {
            throw new InvalidOperationException($"Radio group '{radio.Name}' needs at least two options.");
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in radio.Options)
        {
            if (!values.Add(option.Value))
            {
                throw new InvalidOperationException($"Radio group '{radio.Name}' has duplicate option value '{option.Value}'.");
            }
        }

        if (radio.SelectedValue is not null && !values.Contains(radio.SelectedValue))
        {
            throw new InvalidOperationException($"Radio group '{radio.Name}' selects '{radio.SelectedValue}' which is not among its options.");
        }
    }
}

internal sealed class CreatedFieldWriter(PortableDocument document, FormAppearanceService appearances)
{
    public List<(int PageIndex, ReferenceObject Reference)> Write(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        List<DocumentObject> fields)
    {
        var created = new List<(int, ReferenceObject)>();
        foreach (var definition in document.FormFields)
        {
            FieldPageValidation.Validate(definition, pageNodes.Count);

            var context = new FormEmitContext(
                writer,
                pageNodes[definition.PageIndex].Reference,
                fields,
                created,
                appearances);
            FormFieldWriter.Emit(definition, context);
        }

        return created;
    }
}

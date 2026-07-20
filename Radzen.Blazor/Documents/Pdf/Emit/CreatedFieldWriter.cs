using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

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

internal sealed class CreatedFieldWriter(Document document, FormAppearanceService appearances)
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
            FormFieldEmitter.Emit(definition, context);
        }

        return created;
    }
}

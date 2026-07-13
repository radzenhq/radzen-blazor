using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

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
            if (definition.PageIndex < 0 || definition.PageIndex >= pageNodes.Count)
            {
                throw new InvalidOperationException(
                    $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {pageNodes.Count} pages.");
            }

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

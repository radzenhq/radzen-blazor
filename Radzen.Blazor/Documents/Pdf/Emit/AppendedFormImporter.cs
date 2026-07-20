using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class AppendedFormImporter(Document document, FormAppearanceService appearances)
{
    private readonly HashSet<string> usedFieldNames = new(StringComparer.Ordinal);

    private LoadedState? Loaded => document.Loaded;

    private DocumentReader? Source => document.Loaded?.Source;

    public List<DocumentObject> Import(
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
        DocumentWriter writer)
    {
        var fields = new List<DocumentObject>();
        RegisterExistingFieldNames();

        foreach (var (page, _, reference) in pageNodes)
        {
            if (Loaded is { } loaded && loaded.AppendedPages.TryGetValue(page, out var appended))
            {
                GraphImporter.GetOrCreate(appendImporters, appended.Reader, writer).Seed(appended.Node, reference);
            }
        }

        foreach (var (page, _, _) in pageNodes)
        {
            if (Loaded is not { } loaded || !loaded.AppendedPages.TryGetValue(page, out var appended))
            {
                continue;
            }

            var reader = appended.Reader;
            var importer = GraphImporter.GetOrCreate(appendImporters, reader, writer);
            if (reader.GetArray(appended.Node, "Annots") is not { } annots)
            {
                continue;
            }

            if (!loaded.AppendedAcroForms.TryGetValue(reader, out var sourceForm))
            {
                continue;
            }

            appearances.RegisterAppendedDefaults(importer, sourceForm);

            for (var i = 0; i < annots.Count; i++)
            {
                if (reader.AsDictionary(annots[i]) is { } annot
                    && importer.TryImportFieldRoot(annot, out var root, out var field, out var name))
                {
                    GraphImporter.DisambiguateFieldName(field!, name, usedFieldNames);
                    fields.Add(root);
                }
            }
        }

        return fields;
    }

    private void RegisterExistingFieldNames()
    {
        foreach (var definition in document.FormFields)
        {
            usedFieldNames.Add(definition.Name);
        }

        var source = Source;
        if (source is not null && Loaded!.SourceAcroForm is { } sourceForm
            && source.GetArray(sourceForm, "Fields") is { } rootFields)
        {
            foreach (var field in rootFields)
            {
                if (source.AsDictionary(field) is { } dict && GraphImporter.DecodedName(source, dict) is { } name)
                {
                    usedFieldNames.Add(name);
                }
            }
        }
    }
}

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
        foreach (var (page, node, reference) in pageNodes)
        {
            if (Loaded is not { } loaded || !loaded.AppendedPages.TryGetValue(page, out var appended))
            {
                continue;
            }

            var reader = appended.Reader;
            if (!appendImporters.TryGetValue(reader, out var importer))
            {
                importer = new GraphImporter(reader, writer);
                appendImporters[reader] = importer;
            }

            importer.Seed(appended.Node, reference);
            if (reader.GetArray(appended.Node, "Annots") is not { } annots)
            {
                continue;
            }

            node["Annots"] = PageAnnotationImport.Import(importer, annots);

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
                if (source.AsDictionary(field) is { } dict && source.GetString(dict, "T") is { } text)
                {
                    usedFieldNames.Add(text);
                }
            }
        }
    }
}

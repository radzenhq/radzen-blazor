using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class PreserveFormRequest
{
    public required GraphImporter Importer { get; init; }

    public required DictionaryObject Catalog { get; init; }

    public required List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> PageNodes { get; init; }

    public required HashSet<DictionaryObject> RemovedPages { get; init; }

    public required List<DocumentObject> AppendedFields { get; init; }

    public required DocumentWriter Writer { get; init; }
}

internal sealed class LoadedFormPreserver(Document document, FormAppearanceService appearances)
{
    private LoadedState? Loaded => document.Loaded;

    private DocumentReader? Source => document.Loaded?.Source;

    public void Preserve(PreserveFormRequest request)
    {
        var source = Source;
        var sourceAcroForm = Loaded?.SourceAcroForm;
        foreach (var (page, node, _) in request.PageNodes)
        {
            if (source is null || !Loaded!.SourcePages.TryGetValue(page, out var sourceNode)
                || source.GetArray(sourceNode, "Annots") is not { } annots)
            {
                continue;
            }

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(request.Importer.ImportValue(annot));
            }

            node["Annots"] = imported;
        }

        if (sourceAcroForm is not null && source is not null)
        {
            request.Catalog["AcroForm"] = ImportAcroForm(request, source, sourceAcroForm);
        }
        else if (request.AppendedFields.Count > 0)
        {
            request.Catalog["AcroForm"] = request.Writer.Add(appearances.FieldsForm(request.AppendedFields));
        }
    }

    private ReferenceObject ImportAcroForm(
        PreserveFormRequest request,
        DocumentReader reader,
        DictionaryObject acroForm)
    {
        var result = new DictionaryObject();
        ArrayObject? fieldsArray = null;
        foreach (var key in acroForm.Keys)
        {
            if (string.Equals(key, "Fields", StringComparison.Ordinal))
            {
                fieldsArray = [];
                if (reader.AsArray(acroForm[key]) is { } fields)
                {
                    foreach (var field in fields)
                    {
                        if (!FieldOnRemovedPage(reader, field, request.RemovedPages))
                        {
                            fieldsArray.Add(request.Importer.ImportValue(field));
                        }
                    }
                }
            }
            else
            {
                result[key] = request.Importer.ImportValue(acroForm[key]);
            }
        }

        fieldsArray ??= [];
        foreach (var field in request.AppendedFields)
        {
            fieldsArray.Add(field);
        }

        result["Fields"] = fieldsArray;
        appearances.ApplyCreatedDefaults(result);

        if (appearances.HasAppendedDefaults && acroForm.ContainsKey("DR"))
        {
            result["DR"] = request.Importer.ImportValue(reader.Resolve(acroForm["DR"]));
        }

        appearances.MergeAppendedDefaults(result);
        return request.Writer.Add(result);
    }

    private static bool FieldOnRemovedPage(
        DocumentReader reader,
        DocumentObject field,
        HashSet<DictionaryObject> removed)
    {
        if (reader.AsDictionary(field) is not { } dict)
        {
            return false;
        }

        if (reader.GetDictionary(dict, "P") is { } page && removed.Contains(page))
        {
            return true;
        }

        if (reader.GetArray(dict, "Kids") is { } kids)
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is { } kidDict
                    && reader.GetDictionary(kidDict, "P") is { } kidPage && removed.Contains(kidPage))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

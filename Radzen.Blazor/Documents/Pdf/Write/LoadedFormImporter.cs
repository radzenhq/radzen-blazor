using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal sealed class PreserveFormRequest
{
    public required GraphImporter Importer { get; init; }

    public required DictionaryObject Catalog { get; init; }

    public required List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> PageNodes { get; init; }

    public required HashSet<DictionaryObject> RemovedPages { get; init; }

    public required List<DocumentObject> AppendedFields { get; init; }

    public required DocumentWriter Writer { get; init; }
}

internal sealed class LoadedFormImporter(PortableDocument document, FormWriter forms)
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

            if (page.IsGenerated)
            {
                var widgets = PageAnnotationImporter.ImportWidgets(request.Importer, source, annots);
                if (widgets.Count == 0)
                {
                    continue;
                }

                if (node.TryGetValue("Annots", out var current) && current is ArrayObject generated)
                {
                    foreach (var widget in widgets)
                    {
                        generated.Add(widget);
                    }
                }
                else
                {
                    node["Annots"] = widgets;
                }

                continue;
            }

            node["Annots"] = PageAnnotationImporter.Import(request.Importer, annots);
        }

        if (sourceAcroForm is not null && source is not null)
        {
            request.Catalog["AcroForm"] = ImportAcroForm(request, source, sourceAcroForm);
        }
        else if (request.AppendedFields.Count > 0)
        {
            request.Catalog["AcroForm"] = request.Writer.Add(forms.FieldsForm(request.AppendedFields));
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
        forms.ApplyCreatedDefaults(result);

        if (forms.HasAppendedDefaults && acroForm.ContainsKey("DR"))
        {
            result["DR"] = request.Importer.ImportValue(reader.Resolve(acroForm["DR"]));
        }

        forms.MergeAppendedDefaults(result);
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

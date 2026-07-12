using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Handles interactive forms on save: flattens loaded widgets and pending field
// definitions into static content, preserves and merges a loaded AcroForm across
// a re-save, imports appended-page form fields, and emits created form fields.
internal sealed class FormWriter(Document document)
{
    public void Flatten()
    {
        foreach (var definition in document.FormFields)
        {
            if (definition.PageIndex < 0 || definition.PageIndex >= document.Pages.Count)
            {
                throw new InvalidOperationException(
                    $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {document.Pages.Count} pages.");
            }

            DrawDefinition(document.Pages[definition.PageIndex], definition);
        }

        document.FormFields.Clear();
        FlattenLoadedForm();
    }

    private static void DrawDefinition(Page page, FormFieldDefinition definition)
    {
        if (definition is TextFieldDefinition text)
        {
            if (text.Value.Length == 0)
            {
                return;
            }

            var baseline = FieldAppearances.Baseline(definition.Height.Point, text.Font.Size);
            page.Content.Add(new TextContent(
                text.Value,
                definition.X + Unit.FromPoint(2.0),
                definition.Y + Unit.FromPoint(baseline))
            {
                Font = text.Font,
            });
        }
        else if (definition is CheckBoxFieldDefinition { Checked: true })
        {
            page.Content.Add(FieldAppearances.CheckMark(
                definition.X.Point, definition.Y.Point, definition.Width.Point, definition.Height.Point));
        }
    }

    // Renders every loaded widget's current value into its page content, strips
    // the widgets from the page /Annots and drops the form so the next save
    // emits no /AcroForm.
    private void FlattenLoadedForm()
    {
        var source = document.source;
        var sourceAcroForm = document.sourceAcroForm;
        if (source is null || sourceAcroForm is null)
        {
            return;
        }

        var formDa = sourceAcroForm.TryGetValue("DA", out var da) && source.Resolve(da!) is StringObject text
            ? text.Value
            : null;

        foreach (var page in document.Pages)
        {
            if (!document.sourcePages.TryGetValue(page, out var node)
                || !node.TryGetValue("Annots", out var annotsObject)
                || source.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var remaining = new ArrayObject();
            var widgets = 0;
            foreach (var entry in annots)
            {
                if (source.Resolve(entry) is DictionaryObject annot && IsWidget(annot))
                {
                    widgets++;
                    DrawWidget(page, annot, formDa);
                }
                else
                {
                    remaining.Add(entry);
                }
            }

            if (widgets > 0)
            {
                node["Annots"] = remaining.Count > 0 ? remaining : (DocumentObject)new NullObject();
            }
        }

        document.sourceAcroForm = null;
        document.AcroForm = null;
    }

    private bool IsWidget(DictionaryObject annot)
    {
        var source = document.source;
        return annot.TryGetValue("Subtype", out var subtype) && source!.Resolve(subtype!) is NameObject name
            && string.Equals(name.Value, "Widget", StringComparison.Ordinal);
    }

    // Draws a widget's current value as static content: a text or choice value
    // in its /DA font, a non-Off button state as the check-mark glyph. A hidden
    // widget (/F bit 2) contributes nothing but is still removed.
    private void DrawWidget(Page page, DictionaryObject widget, string? formDa)
    {
        var source = document.source;
        if (widget.TryGetValue("F", out var f) && source!.Resolve(f!) is NumberObject flags
            && (flags.IntValue & 2) != 0)
        {
            return;
        }

        if (Inherited(widget, "FT") is not NameObject type)
        {
            return;
        }

        var (x, y, width, height) = WidgetRect(widget);
        if (string.Equals(type.Value, "Btn", StringComparison.Ordinal))
        {
            var state = widget.TryGetValue("AS", out var asObject) && source!.Resolve(asObject!) is NameObject asName
                ? asName.Value
                : (Inherited(widget, "V") as NameObject)?.Value;
            if (state is not null && !string.Equals(state, "Off", StringComparison.Ordinal))
            {
                page.Content.Add(FieldAppearances.CheckMark(x, y, width, height));
            }

            return;
        }

        if (type.Value is not ("Tx" or "Ch"))
        {
            return;
        }

        var value = Inherited(widget, "V") is StringObject stored
            ? FormField.DecodeTextString(stored.Value)
            : null;
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var da = (Inherited(widget, "DA") as StringObject)?.Value ?? formDa;
        var (daFont, daSize) = FieldAppearances.ParseDefaultAppearance(da);
        var font = FieldAppearances.AppearanceFont(daFont, daSize > 0.0 ? daSize : FieldAppearances.DefaultFontSize);
        page.Content.Add(new TextContent(
            value,
            Unit.FromPoint(x + 2.0),
            Unit.FromPoint(y + FieldAppearances.Baseline(height, font.Size)))
        {
            Font = font,
        });
    }

    // Walks the widget's /Parent chain for an inheritable field attribute
    // (ISO 32000-1 12.7.3.1) and returns it resolved.
    private DocumentObject? Inherited(DictionaryObject widget, string key)
    {
        var source = document.source;
        var current = widget;
        for (var depth = 0; current is not null && depth < 32; depth++)
        {
            if (current.TryGetValue(key, out var value))
            {
                return source!.Resolve(value!);
            }

            current = current.TryGetValue("Parent", out var parent)
                && source!.Resolve(parent!) is DictionaryObject next
                ? next
                : null;
        }

        return null;
    }

    private (double X, double Y, double Width, double Height) WidgetRect(DictionaryObject widget)
    {
        var source = document.source;
        if (widget.TryGetValue("Rect", out var rectObject) && source!.Resolve(rectObject!) is ArrayObject rect
            && rect.Count >= 4)
        {
            var x0 = DocumentLoader.Number(source.Resolve(rect[0]));
            var y0 = DocumentLoader.Number(source.Resolve(rect[1]));
            var x1 = DocumentLoader.Number(source.Resolve(rect[2]));
            var y1 = DocumentLoader.Number(source.Resolve(rect[3]));
            return (Math.Min(x0, x1), Math.Min(y0, y1), Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        }

        return (0.0, 0.0, 0.0, 0.0);
    }

    // Carries the loaded interactive form across a save: widget /Annots stay on
    // their pages and the catalog keeps its /AcroForm, both pointing at the same
    // (possibly mutated) field objects. Fields whose widget lived on a removed
    // page are dropped so the deleted page never re-enters through a /P link.
    public void PreserveForm(GraphImporter importer, DictionaryObject catalog, List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes, HashSet<DictionaryObject> removed, List<DocumentObject> appendedFields, DocumentWriter writer)
    {
        var source = document.source;
        var sourceAcroForm = document.sourceAcroForm;
        foreach (var (page, node, _) in pageNodes)
        {
            if (source is null || !document.sourcePages.TryGetValue(page, out var sourceNode)
                || !sourceNode.TryGetValue("Annots", out var annotsObject)
                || source.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(importer.ImportValue(annot));
            }

            node["Annots"] = imported;
        }

        if (sourceAcroForm is not null && source is not null)
        {
            catalog["AcroForm"] = ImportAcroForm(importer, source, sourceAcroForm, removed, appendedFields, writer);
        }
        else if (appendedFields.Count > 0)
        {
            catalog["AcroForm"] = writer.Add(FieldsForm(appendedFields));
        }
    }

    // Rebuilds the AcroForm field-by-field: source fields whose widget lived on a
    // removed page are dropped, and already-imported fields from appended pages are
    // added, so the merged form lists exactly the fields that still have a widget.
    private ReferenceObject ImportAcroForm(GraphImporter importer, DocumentReader reader, DictionaryObject acroForm, HashSet<DictionaryObject> removed, List<DocumentObject> appendedFields, DocumentWriter writer)
    {
        var result = new DictionaryObject();
        ArrayObject? fieldsArray = null;
        foreach (var key in acroForm.Keys)
        {
            if (string.Equals(key, "Fields", StringComparison.Ordinal))
            {
                fieldsArray = [];
                if (reader.Resolve(acroForm[key]) is ArrayObject fields)
                {
                    foreach (var field in fields)
                    {
                        if (!FieldOnRemovedPage(reader, field, removed))
                        {
                            fieldsArray.Add(importer.ImportValue(field));
                        }
                    }
                }
            }
            else
            {
                result[key] = importer.ImportValue(acroForm[key]);
            }
        }

        fieldsArray ??= [];
        foreach (var field in appendedFields)
        {
            fieldsArray.Add(field);
        }

        result["Fields"] = fieldsArray;
        ApplyCreatedFormDefaults(result);
        return writer.Add(result);
    }

    public DictionaryObject FieldsForm(List<DocumentObject> fieldRefs)
    {
        var fields = new ArrayObject();
        foreach (var field in fieldRefs)
        {
            fields.Add(field);
        }

        var form = new DictionaryObject { ["Fields"] = fields };
        ApplyCreatedFormDefaults(form);
        return form;
    }

    // Gives a form holding created fields the defaults an editor needs: a form
    // /DA, the /DR fonts the created field /DA strings name, and /NeedAppearances
    // when a value falls outside WinAnsi so viewers regenerate it with a capable
    // font. No-op when no fields were created, keeping untouched saves identical.
    private void ApplyCreatedFormDefaults(DictionaryObject form)
    {
        if (document.FormFields.Count == 0)
        {
            return;
        }

        if (!form.ContainsKey("DA"))
        {
            form["DA"] = new StringObject("/Helv 0 Tf 0 g");
        }

        if (!form.ContainsKey("DR"))
        {
            var fonts = new DictionaryObject { ["Helv"] = PageResourceBuilder.Base14FontDictionary("Helvetica") };
            foreach (var definition in document.FormFields)
            {
                if (definition is TextFieldDefinition text)
                {
                    var baseFont = BaseFontOf(text.Font);
                    if (!fonts.ContainsKey(baseFont))
                    {
                        fonts[baseFont] = PageResourceBuilder.Base14FontDictionary(baseFont);
                    }
                }
            }

            form["DR"] = new DictionaryObject { ["Font"] = fonts };
        }

        foreach (var definition in document.FormFields)
        {
            if (definition is TextFieldDefinition text && !FieldAppearances.CanEncode(text.Value))
            {
                form["NeedAppearances"] = new BooleanObject(true);
                break;
            }
        }
    }

    private static string BaseFontOf(Font font)
        => Fonts.Base14Metrics.Resolve(font)?.PostScriptName ?? "Helvetica";

    // Emits one merged field/widget annotation per FormFields definition, each
    // with a generated normal appearance, and lists it in the form fields. The
    // returned page bindings attach to the page /Annots after any preserved
    // form rebuilds them.
    public List<(int PageIndex, ReferenceObject Reference)> WriteCreatedFields(
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

            var x = definition.X.Point;
            var y = definition.Y.Point;
            var width = definition.Width.Point;
            var height = definition.Height.Point;
            var widget = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Widget"),
                ["T"] = new StringObject(definition.Name),
                ["Rect"] = new ArrayObject
                {
                    new NumberObject(x),
                    new NumberObject(y),
                    new NumberObject(x + width),
                    new NumberObject(y + height),
                },
                ["F"] = new NumberObject(4),
                ["P"] = pageNodes[definition.PageIndex].Reference,
            };

            if (definition is TextFieldDefinition text)
            {
                widget["FT"] = new NameObject("Tx");
                widget["V"] = new StringObject(text.Value);
                widget["DA"] = new StringObject(
                    "/" + BaseFontOf(text.Font)
                    + " " + text.Font.Size.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + " Tf 0 g");
                if (FieldAppearances.CanEncode(text.Value))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = writer.Add(FieldAppearances.BuildText(text.Value, width, height, text.Font)),
                    };
                }
            }
            else if (definition is CheckBoxFieldDefinition checkBox)
            {
                var state = checkBox.Checked ? "Yes" : "Off";
                widget["FT"] = new NameObject("Btn");
                widget["V"] = new NameObject(state);
                widget["AS"] = new NameObject(state);
                widget["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        ["Yes"] = writer.Add(FieldAppearances.BuildCheck(width, height)),
                        ["Off"] = writer.Add(FieldAppearances.BuildOff(width, height)),
                    },
                };
            }

            var reference = writer.Add(widget);
            fields.Add(reference);
            created.Add((definition.PageIndex, reference));
        }

        return created;
    }

    // Imports the /Annots of every appended loaded page (seeding its new page ref
    // so annotation /P links repoint) and returns the merged widget/field objects
    // to add to the combined AcroForm.
    public List<DocumentObject> AppendForms(List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes, Dictionary<DocumentReader, GraphImporter> appendImporters, DocumentWriter writer)
    {
        var fields = new List<DocumentObject>();
        foreach (var (page, node, reference) in pageNodes)
        {
            if (!document.appendedPages.TryGetValue(page, out var appended))
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
            if (!appended.Node.TryGetValue("Annots", out var annotsObject)
                || reader.Resolve(annotsObject!) is not ArrayObject annots)
            {
                continue;
            }

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(importer.ImportValue(annot));
            }

            node["Annots"] = imported;

            if (!document.appendedAcroForms.ContainsKey(reader))
            {
                continue;
            }

            for (var i = 0; i < annots.Count; i++)
            {
                if (reader.Resolve(annots[i]) is DictionaryObject annot && IsMergedFormField(annot))
                {
                    fields.Add(imported[i]);
                }
            }
        }

        return fields;
    }

    // A merged widget/field (ISO 32000-1 12.7.3.1): a /Widget annotation that also
    // carries the field's /FT and is not a child of another field.
    private static bool IsMergedFormField(DictionaryObject annot)
        => annot.TryGetValue("Subtype", out var subtype) && subtype is NameObject name
            && string.Equals(name.Value, "Widget", StringComparison.Ordinal)
            && annot.ContainsKey("FT") && !annot.ContainsKey("Parent");

    // A form field is bound to a page through the /P of its own widget (a merged
    // field/widget) or of any widget in its /Kids.
    private static bool FieldOnRemovedPage(DocumentReader reader, DocumentObject field, HashSet<DictionaryObject> removed)
    {
        if (reader.Resolve(field) is not DictionaryObject dict)
        {
            return false;
        }

        if (dict.TryGetValue("P", out var p) && reader.Resolve(p!) is DictionaryObject page && removed.Contains(page))
        {
            return true;
        }

        if (dict.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids)
        {
            foreach (var kid in kids)
            {
                if (reader.Resolve(kid) is DictionaryObject kidDict
                    && kidDict.TryGetValue("P", out var kidP)
                    && reader.Resolve(kidP!) is DictionaryObject kidPage && removed.Contains(kidPage))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

// Handles interactive forms on save: flattens loaded widgets and pending field
// definitions into static content, preserves and merges a loaded AcroForm across
// a re-save, imports appended-page form fields, and emits created form fields.
internal sealed class FormWriter(Document document)
{
    // Field flags (ISO 32000-1 table 226/229): /Ff bit 16 marks a radio group,
    // bit 18 a combo box. Text-field flags: bit 13 multiline, bit 14 password,
    // bit 25 comb.
    internal const int RadioFlag = 1 << 15;
    internal const int ComboFlag = 1 << 17;
    private const int MultilineFlag = 1 << 12;
    private const int PasswordFlag = 1 << 13;
    private const int CombFlag = 1 << 24;

    // Names already claimed by base-source or created fields, so appended nested
    // field trees can be disambiguated deterministically on collision.
    private readonly HashSet<string> usedFieldNames = new(StringComparer.Ordinal);

    // Appended sources whose /DR, /DA and /NeedAppearances must merge into the
    // final AcroForm once its dictionary is assembled.
    private readonly List<(GraphImporter Importer, DictionaryObject Form)> appendedFormDefaults = [];

    private LoadedState? Loaded => document.Loaded;

    private DocumentReader? Source => document.Loaded?.Source;

    public void Flatten()
    {
        foreach (var definition in document.FormFields)
        {
            if (definition.PageIndex < 0 || definition.PageIndex >= document.Pages.Count)
            {
                throw new InvalidOperationException(
                    $"Form field '{definition.Name}' targets page {definition.PageIndex}; the document has {document.Pages.Count} pages.");
            }

            definition.WriteFlattenedContent(document.Pages[definition.PageIndex]);
        }

        document.FormFields.Clear();
        FlattenLoadedForm();
    }

    // Renders every loaded widget's current value into its page content, strips
    // the widgets from the page /Annots and drops the form so the next save
    // emits no /AcroForm.
    private void FlattenLoadedForm()
    {
        var source = Source;
        var sourceAcroForm = Loaded?.SourceAcroForm;
        if (source is null || sourceAcroForm is null)
        {
            return;
        }

        var formDa = source.GetString(sourceAcroForm, "DA");

        foreach (var page in document.Pages)
        {
            if (!Loaded!.SourcePages.TryGetValue(page, out var node)
                || source.GetArray(node, "Annots") is not { } annots)
            {
                continue;
            }

            var remaining = new ArrayObject();
            var widgets = 0;
            foreach (var entry in annots)
            {
                if (source.AsDictionary(entry) is { } annot && IsWidget(annot))
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

        Loaded!.ClearAcroForm();
        document.AcroForm = null;
    }

    private bool IsWidget(DictionaryObject annot)
    {
        var source = Source;
        return string.Equals(source!.GetName(annot, "Subtype"), "Widget", StringComparison.Ordinal);
    }

    // Draws a widget's current value as static content: a text or choice value
    // in its /DA font, a non-Off button state as the check-mark glyph. A hidden
    // widget (/F bit 2) contributes nothing but is still removed.
    private void DrawWidget(Page page, DictionaryObject widget, string? formDa)
    {
        var source = Source;
        if (source!.GetInt(widget, "F") is { } flags && (flags & 2) != 0)
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
            var state = source!.GetName(widget, "AS") ?? (Inherited(widget, "V") as NameObject)?.Value;
            if (state is not null && !string.Equals(state, "Off", StringComparison.Ordinal))
            {
                var radio = Inherited(widget, "Ff") is NumberObject ff && (ff.IntValue & RadioFlag) != 0;
                page.Content.Add(radio
                    ? FieldAppearances.RadioDot(x, y, width, height)
                    : FieldAppearances.CheckMark(x, y, width, height));
            }

            return;
        }

        if (type.Value is not ("Tx" or "Ch"))
        {
            // A /Sig (or other) widget's visible appearance cannot be reproduced by the
            // redraw heuristic; flattening it would silently erase what the viewer showed.
            if (HasVisibleAppearance(widget))
            {
                throw new NotSupportedException(
                    $"Cannot flatten a /{type.Value} field with a visible appearance.");
            }

            return;
        }

        var value = ChoiceOrTextValue(Inherited(widget, "V"));
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

    // The flattenable text of a /Tx or /Ch value: a string, or a multi-select list box's
    // array of selected export values joined onto one line so the selections are not lost.
    private string? ChoiceOrTextValue(DocumentObject? value)
    {
        var source = Source;
        switch (value)
        {
            case StringObject stored:
                return FormField.DecodeTextString(stored.Value);
            case ArrayObject items:
                var parts = new List<string>();
                foreach (var item in items)
                {
                    if (source!.AsString(item) is { } text)
                    {
                        parts.Add(FormField.DecodeTextString(text));
                    }
                }

                return string.Join(", ", parts);
            default:
                return null;
        }
    }

    private bool HasVisibleAppearance(DictionaryObject widget)
    {
        var source = Source;
        return source!.GetDictionary(widget, "AP") is { } ap
            && source!.GetStream(ap, "N") is { } stream
            && stream.Data.Length > 0;
    }

    // Walks the widget's /Parent chain for an inheritable field attribute
    // (ISO 32000-1 12.7.3.1) and returns it resolved.
    private DocumentObject? Inherited(DictionaryObject widget, string key)
    {
        var source = Source;
        var current = widget;
        for (var depth = 0; current is not null && depth < 32; depth++)
        {
            if (current.TryGetValue(key, out var value))
            {
                return source!.Resolve(value!);
            }

            current = source!.GetDictionary(current, "Parent");
        }

        return null;
    }

    private (double X, double Y, double Width, double Height) WidgetRect(DictionaryObject widget)
    {
        var source = Source;
        if (source!.GetArray(widget, "Rect") is { } rect && rect.Count >= 4)
        {
            var x0 = source!.AsNumber(rect[0]) ?? 0.0;
            var y0 = source!.AsNumber(rect[1]) ?? 0.0;
            var x1 = source!.AsNumber(rect[2]) ?? 0.0;
            var y1 = source!.AsNumber(rect[3]) ?? 0.0;
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
        var source = Source;
        var sourceAcroForm = Loaded?.SourceAcroForm;
        foreach (var (page, node, _) in pageNodes)
        {
            if (source is null || !Loaded!.SourcePages.TryGetValue(page, out var sourceNode)
                || source.GetArray(sourceNode, "Annots") is not { } annots)
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
                if (reader.AsArray(acroForm[key]) is { } fields)
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

        // Re-inline the base /DR so appended sources can union their fonts into it
        // (MergeFormDefaults no-ops when /DR is an indirect reference).
        if (appendedFormDefaults.Count > 0 && acroForm.ContainsKey("DR"))
        {
            result["DR"] = importer.ImportValue(reader.Resolve(acroForm["DR"]));
        }

        MergeAppendedFormDefaults(result);
        return writer.Add(result);
    }

    // Unions the /DR, /DA and /NeedAppearances of every appended source form into
    // the final AcroForm dictionary.
    private void MergeAppendedFormDefaults(DictionaryObject form)
    {
        foreach (var (importer, sourceForm) in appendedFormDefaults)
        {
            importer.MergeFormDefaults(form, sourceForm);
        }
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
        MergeAppendedFormDefaults(form);
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
                if (definition.TextAppearance is (_, { } font))
                {
                    var baseFont = BaseFontOf(font);
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
            if (definition.TextAppearance is ({ } value, _) && !FieldAppearances.CanEncode(value))
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

            definition.EmitCreatedField(writer, pageNodes[definition.PageIndex].Reference, fields, created);
        }

        return created;
    }

    internal static int TextFieldFlags(TextFieldDefinition text)
    {
        var flags = 0;
        if (text.Multiline)
        {
            flags |= MultilineFlag;
        }

        if (text.Password)
        {
            flags |= PasswordFlag;
        }

        if (text.Comb)
        {
            flags |= CombFlag;
        }

        return flags;
    }

    internal static string DefaultAppearanceOf(Font font)
        => "/" + BaseFontOf(font)
            + " " + font.Size.ToString("0.###", CultureInfo.InvariantCulture)
            + " Tf 0 g";

    // Imports the /Annots of every appended loaded page (seeding its new page ref
    // so annotation /P links repoint) and returns the merged widget/field objects
    // to add to the combined AcroForm.
    public List<DocumentObject> AppendForms(List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes, Dictionary<DocumentReader, GraphImporter> appendImporters, DocumentWriter writer)
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

            var imported = new ArrayObject();
            foreach (var annot in annots)
            {
                imported.Add(importer.ImportValue(annot));
            }

            node["Annots"] = imported;

            if (!loaded.AppendedAcroForms.TryGetValue(reader, out var sourceForm))
            {
                continue;
            }

            appendedFormDefaults.Add((importer, sourceForm));

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

    // Seeds usedFieldNames with the top-level /T names already committed by the
    // base source form and the created field definitions, so appended trees are
    // the ones renamed on collision (base/created names stay stable).
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

    // A form field is bound to a page through the /P of its own widget (a merged
    // field/widget) or of any widget in its /Kids.
    private static bool FieldOnRemovedPage(DocumentReader reader, DocumentObject field, HashSet<DictionaryObject> removed)
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

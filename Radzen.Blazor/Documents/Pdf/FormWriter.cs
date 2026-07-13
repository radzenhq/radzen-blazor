using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf;

// Handles interactive forms on save: flattens loaded widgets and pending field
// definitions into static content, preserves and merges a loaded AcroForm across
// a re-save, imports appended-page form fields, and emits created form fields.
internal sealed class FormWriter(Document document)
{
    // Field flags (ISO 32000-1 table 226/229): /Ff bit 16 marks a radio group,
    // bit 18 a combo box. Text-field flags: bit 13 multiline, bit 14 password,
    // bit 25 comb.
    private const int RadioFlag = 1 << 15;
    private const int ComboFlag = 1 << 17;
    private const int MultilineFlag = 1 << 12;
    private const int PasswordFlag = 1 << 13;
    private const int CombFlag = 1 << 24;

    // Names already claimed by base-source or created fields, so appended nested
    // field trees can be disambiguated deterministically on collision.
    private readonly HashSet<string> usedFieldNames = new(StringComparer.Ordinal);

    // Appended sources whose /DR, /DA and /NeedAppearances must merge into the
    // final AcroForm once its dictionary is assembled.
    private readonly List<(GraphImporter Importer, DictionaryObject Form)> appendedFormDefaults = [];

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
        else if (definition is RadioGroupFieldDefinition radio)
        {
            var selected = radio.SelectedValue is null
                ? null
                : radio.Options.Find(option => string.Equals(option.Value, radio.SelectedValue, StringComparison.Ordinal));
            if (selected is not null)
            {
                page.Content.Add(FieldAppearances.RadioDot(
                    selected.X.Point, selected.Y.Point, selected.Width.Point, selected.Height.Point));
            }
        }
        else if (definition is ChoiceFieldDefinition choice && choice.Value.Length > 0)
        {
            var baseline = FieldAppearances.Baseline(definition.Height.Point, choice.Font.Size);
            page.Content.Add(new TextContent(
                choice.Value,
                definition.X + Unit.FromPoint(2.0),
                definition.Y + Unit.FromPoint(baseline))
            {
                Font = choice.Font,
            });
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
        var source = document.source;
        switch (value)
        {
            case StringObject stored:
                return FormField.DecodeTextString(stored.Value);
            case ArrayObject items:
                var parts = new List<string>();
                foreach (var item in items)
                {
                    if (source!.Resolve(item) is StringObject text)
                    {
                        parts.Add(FormField.DecodeTextString(text.Value));
                    }
                }

                return string.Join(", ", parts);
            default:
                return null;
        }
    }

    private bool HasVisibleAppearance(DictionaryObject widget)
    {
        var source = document.source;
        return widget.TryGetValue("AP", out var apObject) && source!.Resolve(apObject!) is DictionaryObject ap
            && ap.TryGetValue("N", out var nObject) && source.Resolve(nObject!) is StreamObject stream
            && stream.Data.Length > 0;
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
                if (TextAppearanceOf(definition) is (_, { } font))
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
            if (TextAppearanceOf(definition) is ({ } value, _) && !FieldAppearances.CanEncode(value))
            {
                form["NeedAppearances"] = new BooleanObject(true);
                break;
            }
        }
    }

    private static (string Value, Font Font)? TextAppearanceOf(FormFieldDefinition definition) => definition switch
    {
        TextFieldDefinition text => (text.Value, text.Font),
        ChoiceFieldDefinition choice => (choice.Value, choice.Font),
        _ => null,
    };

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

            if (definition is RadioGroupFieldDefinition radio)
            {
                WriteRadioGroup(writer, pageNodes[definition.PageIndex].Reference, radio, fields, created);
                continue;
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
                widget["DA"] = new StringObject(DefaultAppearanceOf(text.Font));
                var flags = TextFieldFlags(text);
                if (flags != 0)
                {
                    widget["Ff"] = new NumberObject(flags);
                }

                if (text.MaxLength is { } maxLength)
                {
                    widget["MaxLen"] = new NumberObject(maxLength);
                }

                if (FieldAppearances.CanEncode(text.Value))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = writer.Add(FieldAppearances.BuildText(text.Value, width, height, text.Font)),
                    };
                }
            }
            else if (definition is ChoiceFieldDefinition choice)
            {
                var options = new ArrayObject();
                foreach (var option in choice.Options)
                {
                    options.Add(new StringObject(option));
                }

                widget["FT"] = new NameObject("Ch");
                widget["Opt"] = options;
                if (choice.ComboBox)
                {
                    widget["Ff"] = new NumberObject(ComboFlag);
                }

                widget["V"] = new StringObject(choice.Value);
                widget["DA"] = new StringObject(DefaultAppearanceOf(choice.Font));
                if (FieldAppearances.CanEncode(choice.Value))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = writer.Add(FieldAppearances.BuildText(choice.Value, width, height, choice.Font)),
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

    // Emits a radio group as a parent /Btn field (Radio flag, /V and /DV holding
    // the selected on-state) with one kid widget per option, each carrying its own
    // /AP /N keyed by the option value plus /Off and an /AS matching the selection.
    private static void WriteRadioGroup(
        DocumentWriter writer,
        ReferenceObject pageReference,
        RadioGroupFieldDefinition radio,
        List<DocumentObject> fields,
        List<(int, ReferenceObject)> created)
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

        var state = radio.SelectedValue ?? "Off";
        var parent = new DictionaryObject
        {
            ["FT"] = new NameObject("Btn"),
            ["T"] = new StringObject(radio.Name),
            ["Ff"] = new NumberObject(RadioFlag),
            ["V"] = new NameObject(state),
            ["DV"] = new NameObject(state),
        };
        var parentReference = writer.Add(parent);

        var kids = new ArrayObject();
        foreach (var option in radio.Options)
        {
            var x = option.X.Point;
            var y = option.Y.Point;
            var width = option.Width.Point;
            var height = option.Height.Point;
            var selected = string.Equals(option.Value, radio.SelectedValue, StringComparison.Ordinal);
            var kid = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Widget"),
                ["Rect"] = new ArrayObject
                {
                    new NumberObject(x),
                    new NumberObject(y),
                    new NumberObject(x + width),
                    new NumberObject(y + height),
                },
                ["F"] = new NumberObject(4),
                ["P"] = pageReference,
                ["Parent"] = parentReference,
                ["AS"] = new NameObject(selected ? option.Value : "Off"),
                ["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        [option.Value] = writer.Add(FieldAppearances.BuildRadio(width, height, selected: true)),
                        ["Off"] = writer.Add(FieldAppearances.BuildRadio(width, height, selected: false)),
                    },
                },
            };

            var kidReference = writer.Add(kid);
            kids.Add(kidReference);
            created.Add((radio.PageIndex, kidReference));
        }

        parent["Kids"] = kids;
        fields.Add(parentReference);
    }

    private static int TextFieldFlags(TextFieldDefinition text)
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

    private static string DefaultAppearanceOf(Font font)
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

            if (!document.appendedAcroForms.TryGetValue(reader, out var sourceForm))
            {
                continue;
            }

            appendedFormDefaults.Add((importer, sourceForm));

            for (var i = 0; i < annots.Count; i++)
            {
                if (reader.Resolve(annots[i]) is DictionaryObject annot
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

        var source = document.source;
        if (source is not null && document.sourceAcroForm is { } sourceForm
            && sourceForm.TryGetValue("Fields", out var fieldsObject)
            && source.Resolve(fieldsObject!) is ArrayObject rootFields)
        {
            foreach (var field in rootFields)
            {
                if (source.Resolve(field) is DictionaryObject dict
                    && dict.TryGetValue("T", out var title)
                    && source.Resolve(title!) is StringObject text)
                {
                    usedFieldNames.Add(text.Value);
                }
            }
        }
    }

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

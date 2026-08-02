using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Output;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Write;

internal sealed record AuthoredFieldEmission(
    List<(int PageIndex, ReferenceObject Reference)> Widgets,
    List<WidgetElementJoin> StructureJoins);

internal static class AuthoredFields
{
    public static IEnumerable<(int PageIndex, int WidgetIndex, OutputWidget Widget)> Placed(
        PortableDocument document, PageOutputMap pageMap)
    {
        if (document.Output is null || document.AuthoredFieldsFlattened)
        {
            yield break;
        }

        for (var pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
        {
            if (pageMap.PlanAt(pageIndex) is not { } plan)
            {
                continue;
            }

            for (var widget = 0; widget < plan.Widgets.Length; widget++)
            {
                yield return (pageIndex, widget, plan.Widgets[widget]);
            }
        }
    }

    // ISO 32000-1 12.7.3.3: the default appearance of a variable-text field names a font from the
    // form's /DR resource dictionary, so a field appearance is drawn with a standard-14 face.
    public static Font AppearanceFont(in FontPaint paint)
        => FieldAppearances.AppearanceFont(paint.Family, paint.Size);
}

internal sealed class AuthoredFieldWriter(PortableDocument document, FormWriter forms)
{
    private readonly Dictionary<string, ArrayObject> radioKids = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DictionaryObject> radioGroups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ReferenceObject> radioReferences = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> radioSelection = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> radioExports = new(StringComparer.Ordinal);
    private readonly HashSet<string> radioIndexedStates = new(StringComparer.Ordinal);
    private readonly HashSet<string> fieldNames = new(StringComparer.Ordinal);

    private bool Claimed => document.Conformance != PdfAConformance.None || document.IsPdfUa;

    public AuthoredFieldEmission Write(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        PageOutputMap pageMap,
        List<DocumentObject> fields)
    {
        var emitted = new List<(int, ReferenceObject)>();
        var joins = new List<WidgetElementJoin>();

        SurveyRadioExports(pageMap);

        foreach (var (pageIndex, widgetIndex, widget) in AuthoredFields.Placed(document, pageMap))
        {
            var page = pageNodes[pageIndex].Reference;
            var dictionary = widget.Field.Kind == FormFieldKind.Radio
                ? RadioButton(writer, page, widget, fields)
                : TerminalField(writer, page, widget);

            var reference = writer.Add(dictionary);
            if (widget.Field.Kind == FormFieldKind.Radio)
            {
                radioKids[widget.Field.Name].Add(reference);
            }
            else
            {
                fields.Add(reference);
            }

            emitted.Add((pageIndex, reference));
            if (widget.StructureElementId is not null)
            {
                joins.Add(new WidgetElementJoin(pageIndex, widgetIndex, dictionary, reference));
            }
        }

        foreach (var (name, group) in radioGroups)
        {
            group["Kids"] = radioKids[name];
            var state = radioSelection.TryGetValue(name, out var selected) ? selected : "Off";
            group["V"] = new NameObject(state);
            group["DV"] = new NameObject(state);
        }

        return new AuthoredFieldEmission(emitted, joins);
    }

    // ISO 32000-1 12.7.4.2.3: a radio group whose /Opt array carries the export values names its
    // widget appearance states by the option's index, freeing the export values from having to be
    // usable as appearance state names.
    private void SurveyRadioExports(PageOutputMap pageMap)
    {
        foreach (var (_, _, widget) in AuthoredFields.Placed(document, pageMap))
        {
            var field = widget.Field;
            if (field.Kind != FormFieldKind.Radio)
            {
                continue;
            }

            if (!radioExports.TryGetValue(field.Name, out var exports))
            {
                exports = [];
                radioExports.Add(field.Name, exports);
            }

            exports.Add(field.Value);
            if (string.Equals(field.Value, "Off", StringComparison.Ordinal))
            {
                radioIndexedStates.Add(field.Name);
            }
        }
    }

    private string RadioState(string name, string value, int index)
        => radioIndexedStates.Contains(name)
            ? index.ToString(CultureInfo.InvariantCulture)
            : value;

    private static DictionaryObject Widget(ReferenceObject page, in OutputWidget widget) => new()
    {
        ["Type"] = new NameObject("Annot"),
        ["Subtype"] = new NameObject("Widget"),
        // ISO 19005-3 6.3.2: Print flag (bit 3 = 4) set, Hidden and NoView clear.
        ["F"] = new NumberObject(4),
        ["Rect"] = PageBoxWriter.NumberBox(
            PdfRect.FromSize(widget.X, widget.Bottom, widget.Field.Width, widget.Field.Height)),
        ["P"] = page,
    };

    private DictionaryObject TerminalField(DocumentWriter writer, ReferenceObject page, in OutputWidget widget)
    {
        var field = widget.Field;
        RequireUniqueName(field.Name);

        var dictionary = Widget(page, widget);
        dictionary["T"] = StringObject.FromText(field.Name);
        Describe(dictionary, field);

        var flags = 0;
        switch (field.Kind)
        {
            case FormFieldKind.Text:
                dictionary["FT"] = new NameObject("Tx");
                dictionary["V"] = StringObject.FromText(field.Value);
                dictionary["DV"] = StringObject.FromText(field.Value);
                WriteVariableText(writer, dictionary, widget);
                break;
            case FormFieldKind.CheckBox:
                var state = field.Chosen ? "Yes" : "Off";
                dictionary["FT"] = new NameObject("Btn");
                dictionary["V"] = new NameObject(state);
                dictionary["DV"] = new NameObject(state);
                dictionary["AS"] = new NameObject(state);
                dictionary["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        ["Yes"] = writer.Add(forms.BuildCheck(field.Width, field.Height)),
                        ["Off"] = writer.Add(forms.BuildOff(field.Width, field.Height)),
                    },
                };
                break;
            default:
                var options = new ArrayObject();
                foreach (var option in field.Options)
                {
                    options.Add(StringObject.FromText(option));
                }

                dictionary["FT"] = new NameObject("Ch");
                dictionary["Opt"] = options;
                dictionary["V"] = StringObject.FromText(field.Value);
                dictionary["DV"] = StringObject.FromText(field.Value);
                WriteVariableText(writer, dictionary, widget);
                flags |= FieldFlags.Combo;
                break;
        }

        if (field.Required)
        {
            flags |= FieldFlags.Required;
        }

        if (flags != 0)
        {
            dictionary["Ff"] = new NumberObject(flags);
        }

        return dictionary;
    }

    private void WriteVariableText(DocumentWriter writer, DictionaryObject dictionary, in OutputWidget widget)
    {
        var field = widget.Field;
        if (forms.EmbeddedText(widget) is { } embedded)
        {
            dictionary["DA"] = new StringObject(embedded.DefaultAppearance);
            dictionary["AP"] = new DictionaryObject { ["N"] = writer.Add(embedded.Appearance) };
            return;
        }

        var font = AuthoredFields.AppearanceFont(widget.Font);
        dictionary["DA"] = new StringObject(forms.RegisterAuthoredFont(font));
        if (Claimed && field.Value.Length == 0)
        {
            dictionary["AP"] = new DictionaryObject
            {
                ["N"] = writer.Add(forms.BuildEmptyAppearance(field.Width, field.Height)),
            };
            return;
        }

        if (forms.CanEncode(field.Value))
        {
            dictionary["AP"] = new DictionaryObject
            {
                ["N"] = writer.Add(forms.BuildAuthoredText(field.Value, field.Width, field.Height, font)),
            };
            return;
        }

        forms.RequireAppearanceRegeneration();
    }

    private DictionaryObject RadioButton(
        DocumentWriter writer,
        ReferenceObject page,
        in OutputWidget widget,
        List<DocumentObject> fields)
    {
        var field = widget.Field;
        if (!radioGroups.ContainsKey(field.Name))
        {
            RequireUniqueName(field.Name);
            var group = new DictionaryObject
            {
                ["FT"] = new NameObject("Btn"),
                ["T"] = StringObject.FromText(field.Name),
                ["Ff"] = new NumberObject(FieldFlags.Radio | (field.Required ? FieldFlags.Required : 0)),
            };
            if (radioIndexedStates.Contains(field.Name))
            {
                var options = new ArrayObject();
                foreach (var export in radioExports[field.Name])
                {
                    options.Add(StringObject.FromText(export));
                }

                group["Opt"] = options;
            }

            radioGroups.Add(field.Name, group);
            radioKids.Add(field.Name, []);
            var groupReference = writer.Add(group);
            radioReferences.Add(field.Name, groupReference);
            fields.Add(groupReference);
        }

        var state = RadioState(field.Name, field.Value, radioKids[field.Name].Count);
        if (field.Chosen)
        {
            radioSelection[field.Name] = state;
        }

        var dictionary = Widget(page, widget);
        dictionary["Parent"] = radioReferences[field.Name];
        Describe(dictionary, field);
        dictionary["AS"] = new NameObject(field.Chosen ? state : "Off");
        dictionary["AP"] = new DictionaryObject
        {
            ["N"] = new DictionaryObject
            {
                [state] = writer.Add(forms.BuildRadio(field.Width, field.Height, selected: true)),
                ["Off"] = writer.Add(forms.BuildRadio(field.Width, field.Height, selected: false)),
            },
        };

        return dictionary;
    }

    // ISO 32000-1 12.7.3.1: /TU is the field's alternate descriptive name, the text assistive
    // technology announces in place of the partial field name.
    private static void Describe(DictionaryObject dictionary, in FormFieldPaint field)
    {
        if (field.Label is { Length: > 0 } label)
        {
            dictionary["TU"] = StringObject.FromText(label);
        }
    }

    private void RequireUniqueName(string name)
    {
        if (!fieldNames.Add(name))
        {
            throw new InvalidOperationException(
                $"Two form fields are named '{name}'; every authored field needs its own name, "
                + "except the buttons of one radio group, which share theirs.");
        }
    }
}

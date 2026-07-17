using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormFlattener(Document document)
{
    // Button /Ff bit 17 (ISO 32000-1 table 226).
    private const int PushButtonFlag = 1 << 16;

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

            WriteCreatedField(document.Pages[definition.PageIndex], definition);
        }

        document.FormFields.Clear();
        FlattenLoadedForm();
    }

    private static void WriteCreatedField(Page page, FormFieldDefinition definition)
    {
        switch (definition)
        {
            case TextFieldDefinition text:
                if (text.Value.Length == 0)
                {
                    return;
                }

                var textBaseline = FieldAppearances.Baseline(text.Height.Point, text.Font.Size);
                page.Content.Add(new TextContent(
                    text.Value,
                    text.X + Unit.FromPoint(2.0),
                    text.Y + Unit.FromPoint(textBaseline))
                {
                    Font = text.Font,
                });
                break;
            case CheckBoxFieldDefinition checkBox:
                if (!checkBox.Checked)
                {
                    return;
                }

                page.Content.Add(FieldAppearances.CheckMark(
                    checkBox.X.Point, checkBox.Y.Point, checkBox.Width.Point, checkBox.Height.Point));
                break;
            case RadioGroupFieldDefinition radio:
                var selected = radio.SelectedValue is null
                    ? null
                    : radio.Options.Find(option => string.Equals(option.Value, radio.SelectedValue, StringComparison.Ordinal));
                if (selected is not null)
                {
                    page.Content.Add(FieldAppearances.RadioDot(
                        selected.X.Point, selected.Y.Point, selected.Width.Point, selected.Height.Point));
                }

                break;
            case ChoiceFieldDefinition choice:
                if (choice.Value.Length == 0)
                {
                    return;
                }

                var choiceBaseline = FieldAppearances.Baseline(choice.Height.Point, choice.Font.Size);
                page.Content.Add(new TextContent(
                    choice.Value,
                    choice.X + Unit.FromPoint(2.0),
                    choice.Y + Unit.FromPoint(choiceBaseline))
                {
                    Font = choice.Font,
                });
                break;
            default:
                throw new NotSupportedException($"Form field definition type '{definition.GetType().FullName}' is not supported.");
        }
    }

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
            // A pushbutton has no /AS or /V state to redraw from, so its caption and border
            // live only in its /AP; refuse rather than delete the widget and paint nothing.
            if (Inherited(widget, "Ff") is NumberObject pushFf
                && (pushFf.IntValue & PushButtonFlag) != 0)
            {
                if (HasVisibleAppearance(widget))
                {
                    throw new NotSupportedException("Cannot flatten a pushbutton field with a visible appearance.");
                }

                return;
            }

            var state = source!.GetName(widget, "AS") ?? (Inherited(widget, "V") as NameObject)?.Value;
            if (state is not null && !string.Equals(state, "Off", StringComparison.Ordinal))
            {
                var radio = Inherited(widget, "Ff") is NumberObject ff && (ff.IntValue & FormFieldEmitter.RadioFlag) != 0;
                page.Content.Add(radio
                    ? FieldAppearances.RadioDot(x, y, width, height)
                    : FieldAppearances.CheckMark(x, y, width, height));
            }

            return;
        }

        if (type.Value is not ("Tx" or "Ch"))
        {
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
        var rect = PdfRect.Read(Source!, Source!.GetArray(widget, "Rect"), RectPolicy.ZeroFallback);
        return (rect.Left, rect.Bottom, rect.Width, rect.Height);
    }
}

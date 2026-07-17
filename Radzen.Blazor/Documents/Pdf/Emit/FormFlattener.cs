using Radzen.Documents.Pdf.Objects;
using System;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormFlattener(Document document)
{
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

    private bool IsWidget(DictionaryObject annot) => FormField.IsWidget(Source!, annot);

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
            if (Inherited(widget, "Ff") is NumberObject pushFf
                && (pushFf.IntValue & FieldFlags.PushButton) != 0)
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
                var radio = Inherited(widget, "Ff") is NumberObject ff && (ff.IntValue & FieldFlags.Radio) != 0;
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

        var value = FormField.ValueText(source!, Inherited(widget, "V"));
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!FieldBakePolicy.CanBakeSingleLine(source!, widget, value)
            || (type.Value is "Ch" && !FieldBakePolicy.HasSingleSelection(source!, widget)))
        {
            throw new NotSupportedException(
                $"Cannot flatten the /{type.Value} field '{FieldName(widget)}': its value does not render as a "
                + "single left-aligned line, so flattening it would paint the wrong content.");
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

    private string FieldName(DictionaryObject widget)
        => Inherited(widget, "T") is StringObject name ? FormField.DecodeTextString(name.Value) : "?";

    private bool HasVisibleAppearance(DictionaryObject widget)
    {
        var source = Source;
        return source!.GetDictionary(widget, "AP") is { } ap
            && source!.GetStream(ap, "N") is { } stream
            && stream.Data.Length > 0;
    }

    private DocumentObject? Inherited(DictionaryObject widget, string key)
        => FormField.InheritedAttribute(Source!, widget, key);

    private (double X, double Y, double Width, double Height) WidgetRect(DictionaryObject widget)
    {
        var rect = PdfRect.Read(Source!, Source!.GetArray(widget, "Rect"), RectPolicy.ZeroFallback);
        return (rect.Left, rect.Bottom, rect.Width, rect.Height);
    }
}

using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal sealed class FormFlatteningWriter(PortableDocument document)
{
    private readonly HashSet<Page> ownedResources = [];

    private LoadedState? Loaded => document.Loaded;

    private DocumentReader? Source => document.Loaded?.Source;

    public void Flatten()
    {
        FlattenAuthoredFields();

        foreach (var definition in document.FormFields)
        {
            FieldPageValidation.Validate(definition, document.Pages.Count);

            WriteCreatedField(document.Pages[definition.PageIndex], definition);
        }

        document.FormFields.Clear();
        FlattenLoadedForm();
    }

    private void FlattenAuthoredFields()
    {
        foreach (var (pageIndex, _, widget) in AuthoredFields.Placed(document, PageOutputMap.Build(document.Pages)))
        {
            PaintAuthoredField(document.Pages[pageIndex], widget);
        }

        document.AuthoredFieldsFlattened = true;
    }

    private static void PaintAuthoredField(Page page, in Output.OutputWidget widget)
    {
        var field = widget.Field;
        switch (field.Kind)
        {
            case LaidOut.FormFieldKind.Text or LaidOut.FormFieldKind.DropDown:
                if (field.Value.Length == 0)
                {
                    return;
                }

                if (!FieldAppearances.CanEncode(field.Value))
                {
                    throw new NotSupportedException(
                        $"Cannot flatten the field '{field.Name}': its value has characters the standard-14 "
                        + "appearance font cannot encode, so flattening it would paint the wrong content.");
                }

                page.Content.Add(FieldAppearances.Text(
                    field.Value,
                    widget.X,
                    widget.Bottom,
                    field.Height,
                    AuthoredFields.AppearanceFont(widget.Font)));
                break;
            case LaidOut.FormFieldKind.CheckBox:
                if (field.Chosen)
                {
                    page.Content.Add(FieldAppearances.CheckMark(
                        widget.X, widget.Bottom, field.Width, field.Height));
                }

                break;
            default:
                foreach (var path in FieldAppearances.RadioVisual(
                    widget.X, widget.Bottom, field.Width, field.Height, field.Chosen))
                {
                    page.Content.Add(path);
                }

                break;
        }
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

                RequireBakeable(text);
                page.Content.Add(FieldAppearances.Text(
                    text.Value, text.X.Point, text.Y.Point, text.Height.Point, text.Font));
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
                RadioGroupValidation.Validate(radio);
                foreach (var option in radio.Options)
                {
                    var selected = string.Equals(option.Value, radio.SelectedValue, StringComparison.Ordinal);
                    foreach (var path in FieldAppearances.RadioVisual(
                        option.X.Point, option.Y.Point, option.Width.Point, option.Height.Point, selected))
                    {
                        page.Content.Add(path);
                    }
                }

                break;
            case ChoiceFieldDefinition choice:
                if (choice.Value.Length == 0)
                {
                    return;
                }

                RequireBakeable(choice);
                page.Content.Add(FieldAppearances.Text(
                    choice.Value, choice.X.Point, choice.Y.Point, choice.Height.Point, choice.Font));
                break;
            default:
                throw new NotSupportedException($"Form field definition type '{definition.GetType().FullName}' is not supported.");
        }
    }

    private static void RequireBakeable(FormFieldDefinition definition)
    {
        if (!FieldAppearances.CanBakeSingleLine(definition))
        {
            throw new NotSupportedException(
                $"Cannot flatten the field '{definition.Name}': its value does not render as a single left-aligned line, "
                + "so flattening it would paint the wrong content.");
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
                    DrawWidget(page, annot, sourceAcroForm);
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

    private void DrawWidget(Page page, DictionaryObject widget, DictionaryObject sourceAcroForm)
    {
        var source = Source;
        if (source!.GetInt(widget, "F") is { } flags && (flags & (int)AnnotationFlags.Hidden) != 0)
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
                if (HasVisibleAppearance(widget, SelectedState(widget)))
                {
                    throw new NotSupportedException("Cannot flatten a pushbutton field with a visible appearance.");
                }

                return;
            }

            var state = SelectedState(widget);
            if (TryPaintButtonAppearance(page, widget, state, x, y, width, height))
            {
                return;
            }

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
            if (HasVisibleAppearance(widget, SelectedState(widget)))
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

        var da = InheritedDefaultAppearance.Resolve(source!, widget, sourceAcroForm);
        var (daFont, daSize, _) = DefaultAppearanceGrammar.Parse(da);
        var font = FieldAppearances.AppearanceFont(daFont, daSize);
        page.Content.Add(FieldAppearances.Text(value, x, y, height, font));
    }

    private bool TryPaintButtonAppearance(
        Page page, DictionaryObject widget, string? state, double x, double y, double width, double height)
    {
        if (NormalAppearance(widget, state) is not { } normal)
        {
            return false;
        }

        return LoadedAppearanceWriter.TryPaint(
            Source!, Loaded!, page, ownedResources, normal.Reference, normal.Stream,
            PdfRect.FromSize(x, y, width, height), "FFlatten");
    }

    private (DocumentObject Reference, StreamObject Stream)? NormalAppearance(DictionaryObject widget, string? state)
    {
        var source = Source!;
        if (source.GetDictionary(widget, "AP") is not { } ap || !ap.TryGetValue("N", out var normal))
        {
            return null;
        }

        var resolved = source.Resolve(normal!);
        if (resolved is StreamObject direct)
        {
            return (normal!, direct);
        }

        if (resolved is DictionaryObject states
            && states.TryGetValue(state ?? "Off", out var entry)
            && source.AsStream(entry!) is { } stateStream)
        {
            return (entry!, stateStream);
        }

        return null;
    }

    private string? SelectedState(DictionaryObject widget)
        => Source!.GetName(widget, "AS") ?? (Inherited(widget, "V") as NameObject)?.Value;

    private string FieldName(DictionaryObject widget)
        => Inherited(widget, "T") is StringObject name ? FormField.DecodeTextString(name.Value) : "?";

    private bool HasVisibleAppearance(DictionaryObject widget, string? state)
        => NormalAppearance(widget, state) is { } normal && normal.Stream.Data.Length > 0;

    private DocumentObject? Inherited(DictionaryObject widget, string key)
        => FormField.InheritedAttribute(Source!, widget, key);

    private (double X, double Y, double Width, double Height) WidgetRect(DictionaryObject widget)
    {
        var rect = RectReader.Read(Source!, Source!.GetArray(widget, "Rect"), RectPolicy.ZeroFallback);
        return (rect.Left, rect.Bottom, rect.Width, rect.Height);
    }
}

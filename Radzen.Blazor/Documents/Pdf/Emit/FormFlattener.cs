using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormFlattener(Document document)
{
    private readonly HashSet<Page> ownedResources = [];

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

    private bool TryPaintButtonAppearance(
        Page page, DictionaryObject widget, string? state, double x, double y, double width, double height)
    {
        var source = Source!;
        if (source.GetDictionary(widget, "AP") is not { } ap || !ap.TryGetValue("N", out var normal))
        {
            return false;
        }

        var resolved = source.Resolve(normal!);
        DocumentObject reference;
        StreamObject appearance;
        if (resolved is StreamObject direct)
        {
            reference = normal!;
            appearance = direct;
        }
        else if (resolved is DictionaryObject states
            && states.TryGetValue(state ?? "Off", out var entry)
            && source.AsStream(entry!) is { } stateStream)
        {
            reference = entry!;
            appearance = stateStream;
        }
        else
        {
            return false;
        }

        if (!AppearanceMatrixIsIdentity(appearance)
            || !TryAppearanceBox(appearance, out var x0, out var y0, out var boxWidth, out var boxHeight))
        {
            return false;
        }

        var xobjects = PrivateXObjects(page);
        var name = "FFlatten";
        while (xobjects.ContainsKey(name))
        {
            name += "z";
        }

        xobjects[name] = reference;
        var scaleX = width / boxWidth;
        var scaleY = height / boxHeight;
        page.Content.Add(new XObjectContent(name)
        {
            Transform = Matrix.FromComponents(scaleX, 0, 0, scaleY, x - x0 * scaleX, y - y0 * scaleY),
        });
        return true;
    }

    private bool AppearanceMatrixIsIdentity(StreamObject appearance)
    {
        if (Source!.GetArray(appearance.Dictionary, "Matrix") is not { } matrix)
        {
            return true;
        }

        double[] identity = [1, 0, 0, 1, 0, 0];
        if (matrix.Count != identity.Length)
        {
            return false;
        }

        for (var i = 0; i < identity.Length; i++)
        {
            if (Source!.AsNumber(matrix[i]) is not { } value || value != identity[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool TryAppearanceBox(StreamObject appearance, out double x0, out double y0, out double width, out double height)
    {
        x0 = y0 = width = height = 0.0;
        if (Source!.GetArray(appearance.Dictionary, "BBox") is not { Count: 4 } box
            || Source!.AsNumber(box[0]) is not { } left
            || Source!.AsNumber(box[1]) is not { } bottom
            || Source!.AsNumber(box[2]) is not { } right
            || Source!.AsNumber(box[3]) is not { } top)
        {
            return false;
        }

        x0 = left;
        y0 = bottom;
        width = right - left;
        height = top - bottom;
        return width != 0.0 && height != 0.0;
    }

    private DictionaryObject PrivateXObjects(Page page)
    {
        var loaded = Loaded!;
        loaded.SourceResources.TryGetValue(page, out var resources);
        if (!ownedResources.Add(page))
        {
            return (DictionaryObject)resources!["XObject"]!;
        }

        var copy = new DictionaryObject();
        var xobjects = new DictionaryObject();
        if (resources is not null)
        {
            foreach (var key in resources.Keys)
            {
                copy[key] = resources[key];
            }

            if (Source!.GetDictionary(resources, "XObject") is { } shared)
            {
                foreach (var key in shared.Keys)
                {
                    xobjects[key] = shared[key];
                }
            }
        }

        copy["XObject"] = xobjects;
        loaded.SourceResources[page] = copy;
        return xobjects;
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

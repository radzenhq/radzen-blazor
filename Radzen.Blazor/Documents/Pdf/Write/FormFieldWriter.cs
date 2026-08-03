using Radzen.Documents.Pdf.Objects;
using System;

namespace Radzen.Documents.Pdf.Write;

internal static class FormFieldWriter
{
    public static void Emit(FormFieldDefinition definition, FormWriteContext context)
    {
        if (definition is RadioGroupFieldDefinition radio)
        {
            EmitRadioGroup(radio, context);
            return;
        }

        if (definition is not PositionedFieldDefinition positioned)
        {
            throw new NotSupportedException($"Form field definition type '{definition.GetType().FullName}' is not supported.");
        }

        var x = positioned.X.Point;
        var y = positioned.Y.Point;
        var width = positioned.Width.Point;
        var height = positioned.Height.Point;
        var widget = new DictionaryObject
        {
            ["Type"] = new NameObject("Annot"),
            ["Subtype"] = new NameObject("Widget"),
            ["T"] = new StringObject(definition.Name),
            ["Rect"] = PageBoxWriter.NumberBox(PdfRect.FromSize(x, y, width, height)),
            ["F"] = new NumberObject(4),
            ["P"] = context.PageReference,
        };

        PopulateWidget(definition, widget, context, width, height);

        var reference = context.Writer.Add(widget);
        context.Fields.Add(reference);
        context.Created.Add((definition.PageIndex, reference));
    }

    private static void PopulateWidget(
        FormFieldDefinition definition,
        DictionaryObject widget,
        FormWriteContext context,
        double width,
        double height)
    {
        switch (definition)
        {
            case TextFieldDefinition text:
                if (text.MaxLength is < 1)
                {
                    throw new InvalidOperationException(
                        $"Form field '{text.Name}' has a MaxLength of {text.MaxLength}; a length limit must be at least 1.");
                }

                // ISO 32000-1 12.7.4.3: the Comb flag shall be used only when MaxLen is present and
                // the Multiline, Password and FileSelect flags are clear.
                if (text.Comb && (text.Multiline || text.Password || text.MaxLength is null))
                {
                    throw new InvalidOperationException(
                        $"Form field '{text.Name}' sets Comb, which requires MaxLength and cannot be combined with Multiline or Password.");
                }

                widget["FT"] = new NameObject("Tx");
                widget["V"] = StringObject.FromText(text.Value);
                widget["DA"] = new StringObject(context.Forms.DefaultAppearanceOf(text.Font));
                var flags = TextFieldFlags(text);
                if (flags != 0)
                {
                    widget["Ff"] = new NumberObject(flags);
                }

                if (text.MaxLength is { } maxLength)
                {
                    widget["MaxLen"] = new NumberObject(maxLength);
                }

                if (FieldAppearances.CanBakeSingleLine(text))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = context.Writer.Add(context.Forms.BuildText(text.Value, width, height, text.Font)),
                    };
                }

                break;
            case CheckBoxFieldDefinition checkBox:
                var state = checkBox.Checked ? "Yes" : "Off";
                widget["FT"] = new NameObject("Btn");
                widget["V"] = new NameObject(state);
                widget["AS"] = new NameObject(state);
                widget["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        ["Yes"] = context.Writer.Add(context.Forms.BuildCheck(width, height)),
                        ["Off"] = context.Writer.Add(context.Forms.BuildOff(width, height)),
                    },
                };
                break;
            case ChoiceFieldDefinition choice:
                var options = new ArrayObject();
                foreach (var option in choice.Options)
                {
                    options.Add(StringObject.FromText(option));
                }

                widget["FT"] = new NameObject("Ch");
                widget["Opt"] = options;
                if (choice.ComboBox)
                {
                    widget["Ff"] = new NumberObject(FieldFlags.Combo);
                }

                widget["V"] = StringObject.FromText(choice.Value);
                widget["DA"] = new StringObject(context.Forms.DefaultAppearanceOf(choice.Font));
                if (FieldAppearances.CanBakeSingleLine(choice))
                {
                    widget["AP"] = new DictionaryObject
                    {
                        ["N"] = context.Writer.Add(context.Forms.BuildText(choice.Value, width, height, choice.Font)),
                    };
                }

                break;
            default:
                throw new NotSupportedException($"Form field definition type '{definition.GetType().FullName}' is not supported.");
        }
    }

    private static void EmitRadioGroup(RadioGroupFieldDefinition radio, FormWriteContext context)
    {
        RadioGroupValidation.Validate(radio);

        var state = radio.SelectedValue ?? "Off";
        var parent = new DictionaryObject
        {
            ["FT"] = new NameObject("Btn"),
            ["T"] = new StringObject(radio.Name),
            ["Ff"] = new NumberObject(FieldFlags.Radio),
            ["V"] = new NameObject(state),
            ["DV"] = new NameObject(state),
        };
        var parentReference = context.Writer.Add(parent);

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
                ["Rect"] = PageBoxWriter.NumberBox(PdfRect.FromSize(x, y, width, height)),
                ["F"] = new NumberObject(4),
                ["P"] = context.PageReference,
                ["Parent"] = parentReference,
                ["AS"] = new NameObject(selected ? option.Value : "Off"),
                ["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        [option.Value] = context.Writer.Add(context.Forms.BuildRadio(width, height, selected: true)),
                        ["Off"] = context.Writer.Add(context.Forms.BuildRadio(width, height, selected: false)),
                    },
                },
            };

            var kidReference = context.Writer.Add(kid);
            kids.Add(kidReference);
            context.Created.Add((radio.PageIndex, kidReference));
        }

        parent["Kids"] = kids;
        context.Fields.Add(parentReference);
    }

    private static int TextFieldFlags(TextFieldDefinition text)
    {
        var flags = 0;
        if (text.Multiline)
        {
            flags |= FieldFlags.Multiline;
        }

        if (text.Password)
        {
            flags |= FieldFlags.Password;
        }

        if (text.Comb)
        {
            flags |= FieldFlags.Comb;
        }

        return flags;
    }
}

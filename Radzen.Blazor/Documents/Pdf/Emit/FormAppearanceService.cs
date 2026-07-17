using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormAppearanceService(Document document)
{
    private readonly List<(GraphImporter Importer, DictionaryObject Form)> appendedFormDefaults = [];

    public bool HasAppendedDefaults => appendedFormDefaults.Count > 0;

    public StreamObject BuildText(string value, double width, double height, Font font)
        => FieldAppearances.BuildText(value, width, height, font, document.FontScope);

    public StreamObject BuildCheck(double width, double height)
        => FieldAppearances.BuildCheck(width, height);

    public StreamObject BuildOff(double width, double height)
        => FieldAppearances.BuildOff(width, height);

    public StreamObject BuildRadio(double width, double height, bool selected)
        => FieldAppearances.BuildRadio(width, height, selected);

    public bool CanEncode(string value) => FieldAppearances.CanEncode(value);

    public string DefaultAppearanceOf(Font font)
        => "/" + BaseFontOf(font)
            + " " + font.Size.ToString("0.###", CultureInfo.InvariantCulture)
            + " Tf 0 g";

    public void RegisterAppendedDefaults(GraphImporter importer, DictionaryObject form)
        => appendedFormDefaults.Add((importer, form));

    public void MergeAppendedDefaults(DictionaryObject form)
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
        ApplyCreatedDefaults(form);
        MergeAppendedDefaults(form);
        return form;
    }

    public void ApplyCreatedDefaults(DictionaryObject form)
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
                if (TextAppearance(definition) is (_, { } font))
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
            if (TextAppearance(definition) is ({ } value, _) && !CanEncode(value))
            {
                form["NeedAppearances"] = new BooleanObject(true);
                break;
            }
        }
    }

    private static (string Value, Font Font)? TextAppearance(FormFieldDefinition definition)
        => definition switch
        {
            TextFieldDefinition text => (text.Value, text.Font),
            ChoiceFieldDefinition choice => (choice.Value, choice.Font),
            _ => null,
        };

    private string BaseFontOf(Font font)
        => Fonts.FontResolution.ResolveBase14Name(font, document.FontScope);
}

using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf.Write;

internal sealed class FormWriter
{
    private readonly PortableDocument document;
    private readonly FormFlatteningWriter flattener;
    private readonly CreatedFieldWriter createdFieldWriter;
    private readonly AppendedFormImporter appendedFormImporter;
    private readonly LoadedFormImporter loadedFormImporter;
    private readonly List<(GraphImporter Importer, DictionaryObject Form)> appendedFormDefaults = [];

    public FormWriter(PortableDocument document)
    {
        this.document = document;
        flattener = new FormFlatteningWriter(document);
        createdFieldWriter = new CreatedFieldWriter(document, this);
        appendedFormImporter = new AppendedFormImporter(document, this);
        loadedFormImporter = new LoadedFormImporter(document, this);
    }

    public void Flatten() => flattener.Flatten();

    public List<DocumentObject> AppendForms(
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
        DocumentWriter writer)
        => appendedFormImporter.Import(pageNodes, appendImporters, writer);

    public List<(int PageIndex, ReferenceObject Reference)> WriteCreatedFields(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        List<DocumentObject> fields)
        => createdFieldWriter.Write(writer, pageNodes, fields);

    public void PreserveForm(PreserveFormRequest request)
        => loadedFormImporter.Preserve(request);

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

    public StreamObject BuildText(string value, double width, double height, Font font)
        => FieldAppearances.BuildText(value, width, height, font, document.FontScope);

    public StreamObject BuildCheck(double width, double height) => FieldAppearances.BuildCheck(width, height);

    public StreamObject BuildOff(double width, double height) => FieldAppearances.BuildOff(width, height);

    public StreamObject BuildRadio(double width, double height, bool selected)
        => FieldAppearances.BuildRadio(width, height, selected);

    public bool CanEncode(string value) => FieldAppearances.CanEncode(value);

    public string DefaultAppearanceOf(Font font)
        => DefaultAppearanceGrammar.Write(BaseFontOf(font), font.EffectiveSize.Point, "0 g");

    public void RegisterAppendedDefaults(GraphImporter importer, DictionaryObject form)
        => appendedFormDefaults.Add((importer, form));

    public bool HasAppendedDefaults => appendedFormDefaults.Count > 0;

    public void MergeAppendedDefaults(DictionaryObject form)
    {
        foreach (var (importer, sourceForm) in appendedFormDefaults)
        {
            importer.MergeFormDefaults(form, sourceForm);
        }
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
            if (TextAppearance(definition) is not null && !FieldAppearances.CanBakeSingleLine(definition))
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

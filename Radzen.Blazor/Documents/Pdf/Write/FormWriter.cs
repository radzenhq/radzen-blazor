using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Output;
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
    private readonly SortedSet<string> authoredFonts = new(System.StringComparer.Ordinal);
    private readonly SortedDictionary<string, DocumentObject> embeddedFonts = new(System.StringComparer.Ordinal);
    private readonly DocumentWriter? objects;
    private readonly Dictionary<OutputFont, DocumentObject>? fontReferences;
    private bool regenerateAppearances;

    public FormWriter(
        PortableDocument document,
        DocumentWriter? objects = null,
        Dictionary<OutputFont, DocumentObject>? fontReferences = null)
    {
        this.document = document;
        this.objects = objects;
        this.fontReferences = fontReferences;
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

    public StreamObject BuildAuthoredText(string value, double width, double height, Font font)
        => FieldAppearances.BuildText(value, width, height, font, scope: default);

    public StreamObject BuildEmptyAppearance(double width, double height)
        => FieldAppearances.BuildEmbeddedText([], new DictionaryObject(), width, height, FieldAppearances.DefaultFontSize);

    public (StreamObject Appearance, string DefaultAppearance)? EmbeddedText(in OutputWidget widget)
    {
        if (!widget.HasEmbeddedAppearance || objects is null || fontReferences is null)
        {
            return null;
        }

        var fonts = new DictionaryObject();
        var spans = new List<(string Key, byte[] Bytes, double XOffset)>(widget.Appearance.Length);
        foreach (var span in widget.Appearance)
        {
            if (!span.Font.IsEmbedded)
            {
                return null;
            }

            var reference = PageResourceBuilder.ResolveFont(objects, span.Font, fontReferences);
            fonts[span.Font.Key] = reference;
            embeddedFonts[span.Font.Key] = reference;
            spans.Add((span.Font.Key, span.Bytes, span.XOffset));
        }

        var size = widget.Font.Size > 0.0 ? widget.Font.Size : FieldAppearances.DefaultFontSize;
        return (
            FieldAppearances.BuildEmbeddedText(spans, fonts, widget.Field.Width, widget.Field.Height, size),
            DefaultAppearanceGrammar.Write(widget.Appearance[0].Font.Key, size, "0 g"));
    }

    public string RegisterAuthoredFont(Font font)
    {
        var baseFont = Fonts.FontResolution.ResolveBase14Name(font, scope: default);
        authoredFonts.Add(baseFont);
        return DefaultAppearanceGrammar.Write(baseFont, font.EffectiveSize.Point, "0 g");
    }

    public void RequireAppearanceRegeneration() => regenerateAppearances = true;

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
        if (document.FormFields.Count == 0 && authoredFonts.Count == 0 && embeddedFonts.Count == 0)
        {
            return;
        }

        if (regenerateAppearances)
        {
            form["NeedAppearances"] = new BooleanObject(true);
        }

        var standardFaces = document.FormFields.Count > 0 || authoredFonts.Count > 0 || embeddedFonts.Count == 0;

        if (!form.ContainsKey("DA"))
        {
            form["DA"] = new StringObject(standardFaces
                ? "/Helv 0 Tf 0 g"
                : DefaultAppearanceGrammar.Write(FirstEmbeddedFont(), 0.0, "0 g"));
        }

        if (!form.ContainsKey("DR"))
        {
            var fonts = new DictionaryObject();
            if (standardFaces)
            {
                fonts["Helv"] = PageResourceBuilder.Base14FontDictionary("Helvetica");
            }

            foreach (var embedded in embeddedFonts)
            {
                fonts[embedded.Key] = embedded.Value;
            }

            foreach (var baseFont in authoredFonts)
            {
                if (!fonts.ContainsKey(baseFont))
                {
                    fonts[baseFont] = PageResourceBuilder.Base14FontDictionary(baseFont);
                }
            }

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

    private string FirstEmbeddedFont()
    {
        foreach (var embedded in embeddedFonts)
        {
            return embedded.Key;
        }

        return "Helv";
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

using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormWriter
{
    private readonly FormFlattener flattener;
    private readonly FormAppearanceService appearances;
    private readonly CreatedFieldWriter createdFieldWriter;
    private readonly AppendedFormImporter appendedFormImporter;
    private readonly LoadedFormPreserver loadedFormPreserver;

    public FormWriter(Document document)
    {
        flattener = new FormFlattener(document);
        appearances = new FormAppearanceService(document);
        createdFieldWriter = new CreatedFieldWriter(document, appearances);
        appendedFormImporter = new AppendedFormImporter(document, appearances);
        loadedFormPreserver = new LoadedFormPreserver(document, appearances);
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
        => loadedFormPreserver.Preserve(request);

    public DictionaryObject FieldsForm(List<DocumentObject> fieldRefs)
        => appearances.FieldsForm(fieldRefs);
}

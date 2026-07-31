using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal sealed class FormWriteContext(
    DocumentWriter writer,
    ReferenceObject pageReference,
    List<DocumentObject> fields,
    List<(int PageIndex, ReferenceObject Reference)> created,
    FormWriter forms)
{
    public DocumentWriter Writer { get; } = writer;

    public ReferenceObject PageReference { get; } = pageReference;

    public List<DocumentObject> Fields { get; } = fields;

    public List<(int PageIndex, ReferenceObject Reference)> Created { get; } = created;

    public FormWriter Forms { get; } = forms;
}

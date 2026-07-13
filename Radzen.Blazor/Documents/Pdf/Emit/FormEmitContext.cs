using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class FormEmitContext(
    DocumentWriter writer,
    ReferenceObject pageReference,
    List<DocumentObject> fields,
    List<(int PageIndex, ReferenceObject Reference)> created,
    FormAppearanceService appearances)
{
    public DocumentWriter Writer { get; } = writer;

    public ReferenceObject PageReference { get; } = pageReference;

    public List<DocumentObject> Fields { get; } = fields;

    public List<(int PageIndex, ReferenceObject Reference)> Created { get; } = created;

    public FormAppearanceService Appearances { get; } = appearances;
}

namespace Radzen.Documents.Pdf.Emit;

internal sealed class CapturedRendererSettings
{
    private CapturedRendererSettings(DocumentRenderer renderer)
    {
        Document = new PortableDocument
        {
            Conformance = renderer.Conformance,
            Accessibility = renderer.Accessibility,
            RoleMap = renderer.RoleMap,
            Encryption = renderer.Encryption,
            CompressOutput = renderer.CompressOutput,
            IncludeDocumentId = renderer.IncludeDocumentId,
            ViewerPreferences = renderer.ViewerPreferences,
        };

        Document.Info.Producer = renderer.Producer;

        foreach (var attachment in renderer.Attachments)
        {
            Document.Attachments.Add(attachment);
        }

        foreach (var item in renderer.Outline)
        {
            Document.Outline.Add(item);
        }

        foreach (var label in renderer.PageLabels)
        {
            Document.PageLabels.Add(label);
        }

        foreach (var field in renderer.FormFields)
        {
            Document.FormFields.Add(field);
        }
    }

    public PortableDocument Document { get; }

    public PdfAConformance Conformance => Document.Conformance;

    public PdfUaConformance Accessibility => Document.Accessibility;

    public RoleMap RoleMap => Document.RoleMap;

    public static CapturedRendererSettings Capture(DocumentRenderer renderer)
        => new(renderer);
}

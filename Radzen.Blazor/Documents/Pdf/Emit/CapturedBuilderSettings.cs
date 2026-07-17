using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// Captures the builder's top-level settings by reference at build time; only DocumentInfo is cloned.
internal sealed class CapturedBuilderSettings
{
    private CapturedBuilderSettings(DocumentBuilder builder)
    {
        Info = builder.Info.Clone();
        Attachments = [.. builder.Attachments];
        Outline = [.. builder.Outline];
        Conformance = builder.Conformance;
        Fonts = builder.Fonts;
        PdfUA = builder.PdfUA;
        Language = builder.Language;
        RoleMap = builder.RoleMap;
        Encryption = builder.Encryption;
        CompressOutput = builder.CompressOutput;
        IncludeDocumentId = builder.IncludeDocumentId;
        ViewerPreferences = builder.ViewerPreferences;
        PageLabels = [.. builder.PageLabels];
        FormFields = [.. builder.FormFields];
    }

    public DocumentInfo Info { get; }

    public IReadOnlyList<Attachment> Attachments { get; }

    public IReadOnlyList<OutlineItem> Outline { get; }

    public PdfAConformance Conformance { get; }

    public FontCollection Fonts { get; }

    public bool PdfUA { get; }

    public string? Language { get; }

    public RoleMap RoleMap { get; }

    public Objects.Encryption.EncryptionOptions? Encryption { get; }

    public bool CompressOutput { get; }

    public bool IncludeDocumentId { get; }

    public ViewerPreferences? ViewerPreferences { get; }

    public IReadOnlyList<PageLabel> PageLabels { get; }

    public IReadOnlyList<FormFieldDefinition> FormFields { get; }

    public static CapturedBuilderSettings Capture(DocumentBuilder builder) => new(builder);

    public Document CreateDocument()
    {
        var document = new Document
        {
            Conformance = Conformance,
            Fonts = Fonts,
            PdfUA = PdfUA,
            Language = Language,
            RoleMap = RoleMap,
            Encryption = Encryption,
            CompressOutput = CompressOutput,
            IncludeDocumentId = IncludeDocumentId,
            ViewerPreferences = ViewerPreferences,
        };
        Info.CopyTo(document.Info);
        foreach (var attachment in Attachments)
        {
            document.Attachments.Add(attachment);
        }
        foreach (var item in Outline)
        {
            document.Outline.Add(item);
        }
        foreach (var label in PageLabels)
        {
            document.PageLabels.Add(label);
        }

        foreach (var field in FormFields)
        {
            document.FormFields.Add(field);
        }

        return document;
    }
}

using System.Collections.Immutable;

namespace Radzen.Documents.Pdf.Render;

internal sealed class RenderRequest
{
    private RenderRequest(DocumentRenderer renderer)
    {
        Conformance = renderer.Conformance;
        Accessibility = renderer.Accessibility;
        RoleMap = renderer.RoleMap;
        Encryption = renderer.Encryption;
        CompressOutput = renderer.CompressOutput;
        IncludeDocumentId = renderer.IncludeDocumentId;
        ViewerPreferences = renderer.ViewerPreferences;
        Producer = renderer.Producer;
        Attachments = [.. renderer.Attachments];
        Outline = [.. renderer.Outline];
        PageLabels = [.. renderer.PageLabels];
        FormFields = [.. renderer.FormFields];
        Decoders = renderer.ImageDecoders;
        AllowUnsupportedCharacters = renderer.AllowUnsupportedCharacters;
        AllowRestrictedEmbedding = renderer.AllowRestrictedEmbedding;
        AllowDegradedFonts = renderer.AllowDegradedFonts;
    }

    public PdfAConformance Conformance { get; }

    public PdfUaConformance Accessibility { get; }

    public RoleMap RoleMap { get; }

    public EncryptionOptions? Encryption { get; }

    public bool CompressOutput { get; }

    public bool IncludeDocumentId { get; }

    public ViewerPreferences? ViewerPreferences { get; }

    public string? Producer { get; }

    public ImmutableArray<Attachment> Attachments { get; }

    public ImmutableArray<OutlineItem> Outline { get; }

    public ImmutableArray<PageLabel> PageLabels { get; }

    public ImmutableArray<FormFieldDefinition> FormFields { get; }

    public ImageDecoders Decoders { get; }

    public bool AllowUnsupportedCharacters { get; }

    public bool AllowRestrictedEmbedding { get; }

    public bool AllowDegradedFonts { get; }

    public static RenderRequest From(DocumentRenderer renderer) => new(renderer);
}

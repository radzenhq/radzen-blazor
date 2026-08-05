namespace Radzen.Documents.Pdf.Render;

internal sealed class RenderRequest
{
    private RenderRequest(DocumentRenderer renderer)
    {
        Conformance = renderer.Conformance;
        Accessibility = renderer.Accessibility;
        RoleMap = renderer.RoleMap.Snapshot();
        Producer = renderer.Producer;
        Decoders = renderer.ImageDecoders;
        UnsupportedCharacters = renderer.UnsupportedCharacters;
        AllowRestrictedEmbedding = renderer.AllowRestrictedEmbedding;
    }

    public PdfAConformance Conformance { get; }

    public PdfUaConformance Accessibility { get; }

    public RoleMap RoleMap { get; }

    public string? Producer { get; }

    public ImageDecoders Decoders { get; }

    public UnsupportedCharacterPolicy UnsupportedCharacters { get; }

    public UnsupportedCharacterLog Unsupported { get; } = new();

    public bool AllowRestrictedEmbedding { get; }

    public static RenderRequest From(DocumentRenderer renderer) => new(renderer);
}

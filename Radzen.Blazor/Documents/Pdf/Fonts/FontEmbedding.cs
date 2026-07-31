using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf.Fonts;

internal static class FontEmbedding
{
    public static void Ensure(
        in FontCollectionSnapshot fonts,
        bool allowRestrictedEmbedding,
        bool allowDegradedFonts)
    {
        foreach (var face in fonts.Faces)
        {
            // ISO/IEC 14496-22 OS/2 fsType: Restricted License Embedding forbids embedding the font
            // without a licence from its foundry.
            face.Face.EnsureEmbeddable(allowRestrictedEmbedding);
            face.Face.EnsureRenderable(allowDegradedFonts);
        }
    }
}

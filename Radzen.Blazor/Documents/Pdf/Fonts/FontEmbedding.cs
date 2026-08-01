using System;
using System.Collections.Generic;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Fonts;

internal static class FontEmbedding
{
    // ISO/IEC 14496-22 OS/2 fsType bit 1: RESTRICTED_LICENSE_EMBEDDING forbids embedding the font
    // without a licence from its foundry.
    private const ushort RestrictedLicenseEmbedding = 0x0002;

    public static bool EmbeddingRestricted(SfntFont face)
        => (face.FsType & RestrictedLicenseEmbedding) != 0;

    public static void Ensure(
        IEnumerable<SfntFont> faces,
        bool allowRestrictedEmbedding,
        bool allowDegradedFonts)
    {
        foreach (var face in faces)
        {
            EnsureEmbeddable(face, allowRestrictedEmbedding);
            EnsureRenderable(face, allowDegradedFonts);
        }
    }

    public static void EnsureEmbeddable(SfntFont face, bool allowRestricted)
    {
        if (EmbeddingRestricted(face) && !allowRestricted)
        {
            throw new InvalidOperationException(
                $"The font '{face.PostScriptName}' has OS/2 fsType 0x{face.FsType:X4} (Restricted License Embedding) and must not be embedded. "
                + "Set DocumentRenderer.AllowRestrictedEmbedding to true to embed it anyway if you hold a license "
                + "that permits it.");
        }
    }

    public static void EnsureRenderable(SfntFont face, bool allowDegraded)
    {
        if (allowDegraded)
        {
            return;
        }

        if (face.IsVariable)
        {
            throw new NotSupportedException(
                $"The font '{face.PostScriptName}' is a variable font; axis selection is not supported, so only its default instance "
                + "would be embedded. Set DocumentRenderer.AllowDegradedFonts to true to embed the default instance anyway.");
        }

        if (face.HasColorTables)
        {
            throw new NotSupportedException(
                $"The font '{face.PostScriptName}' is a color font (COLR/sbix/SVG); color glyphs are not supported and would render as "
                + "monochrome outlines or missing. Set DocumentRenderer.AllowDegradedFonts to true to embed it anyway.");
        }
    }
}

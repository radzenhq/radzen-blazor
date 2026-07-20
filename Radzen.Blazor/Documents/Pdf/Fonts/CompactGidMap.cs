using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Fonts;

internal static class CompactGidMap
{
    public static Dictionary<ushort, ushort> Build(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
        => font.IsCff
            ? CffSubsetter.BuildCompactGidMap(glyphIds)
            : GlyfSubsetter.BuildCompactGidMap(font, glyphIds);
}

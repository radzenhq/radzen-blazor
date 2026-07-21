using System;
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

    public static List<ushort> OrderFromMap(IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var ordered = new ushort[gidMap.Count];
        var seen = new bool[gidMap.Count];
        foreach (var (gid, compact) in gidMap)
        {
            if (compact >= gidMap.Count || seen[compact])
            {
                throw new ArgumentException("Compact gid map must be a bijection onto [0, N).", nameof(gidMap));
            }

            ordered[compact] = gid;
            seen[compact] = true;
        }

        return [.. ordered];
    }
}

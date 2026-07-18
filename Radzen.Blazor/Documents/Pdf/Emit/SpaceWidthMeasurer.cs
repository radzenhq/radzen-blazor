using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class SpaceWidthMeasurer
{
    public static double SpaceWidth(FontCollection fonts, Font font, Dictionary<Font, double> cache)
    {
        if (!cache.TryGetValue(font, out var width))
        {
            width = fonts.MeasureText(" ", font);
            cache[font] = width;
        }

        return width;
    }
}

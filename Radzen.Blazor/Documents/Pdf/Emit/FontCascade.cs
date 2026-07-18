using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class FontCascade
{
    public static Font Resolve(IEnumerable<Font?> sources)
    {
        var font = new Font();
        foreach (var source in sources)
        {
            if (source != null)
            {
                font.InheritFrom(source);
            }
        }

        return font;
    }
}

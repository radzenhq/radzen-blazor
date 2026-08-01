using System.Collections.Generic;
using Radzen.Documents.Fonts;

namespace Radzen.Documents;

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

using System;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts;

internal static class CodeRangeExpander
{
    public static void Expand(int start, int end, long existingCount, long limit, string limitMessage, Action<int> emit)
    {
        if (start < 0 || end == int.MaxValue)
        {
            throw new DocumentParseException("A font code range endpoint is outside the addressable code range.");
        }

        if (start > end)
        {
            return;
        }

        var span = (long)end - start + 1;
        if (existingCount + span > limit)
        {
            throw new DocumentParseException(limitMessage);
        }

        for (var code = start; code <= end; code++)
        {
            emit(code);
        }
    }
}

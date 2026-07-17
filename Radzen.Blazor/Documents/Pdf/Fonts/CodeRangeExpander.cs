using System;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Fonts;

// Materializes an inclusive [start, end] run of integer codes shared by the CID font /W
// range form and the incremental /ToUnicode bfrange form.
internal static class CodeRangeExpander
{
    // A code at or near int.MaxValue is an obviously invalid CID/char code: a singleton such
    // range (e.g. /W [2147483647 2147483647 500] or bfrange <7fffffff> <7fffffff>) declares a
    // span of 1 and passes the size cap, but an int loop counter then wraps past the end and
    // never terminates. Reject the endpoint rather than widen the counter and iterate garbage.
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

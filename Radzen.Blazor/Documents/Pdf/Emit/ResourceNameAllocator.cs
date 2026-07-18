using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class ResourceNameAllocator
{
    internal static string Available(string baseName, IEnumerable<string>? reserved, bool reservesPrefix)
    {
        if (reserved is null)
        {
            return baseName;
        }

        var candidate = baseName;
        while (Collides(candidate, reserved, reservesPrefix))
        {
            candidate += "z";
        }

        return candidate;
    }

    private static bool Collides(string candidate, IEnumerable<string> reserved, bool reservesPrefix)
    {
        foreach (var name in reserved)
        {
            if (reservesPrefix
                ? name.StartsWith(candidate, StringComparison.Ordinal)
                : string.Equals(name, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

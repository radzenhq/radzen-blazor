using System;

namespace Radzen.Documents.Pdf.Objects;

internal static class FieldNameUniquifier
{
    public static string MakeUnique(string name, Func<string, bool> isUsed)
    {
        if (!isUsed(name))
        {
            return name;
        }

        var index = 2;
        string candidate;
        do
        {
            candidate = name + "_" + index++;
        }
        while (isUsed(candidate));

        return candidate;
    }
}

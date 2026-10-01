using System;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal static class SheetNameFormat
{
    public static string Quote(string name)
    {
        return NeedsQuoting(name) ? $"'{name.Replace("'", "''", StringComparison.Ordinal)}'" : name;
    }

    public static bool NeedsQuoting(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return true;
        }

        if (!Rune.TryGetRuneAt(name, 0, out var first) || !(Rune.IsLetter(first) || first.Value == '_'))
        {
            return true;
        }

        for (var i = 0; i < name.Length;)
        {
            if (!Rune.TryGetRuneAt(name, i, out var rune) || !(Rune.IsLetterOrDigit(rune) || rune.Value == '_'))
            {
                return true;
            }

            i += rune.Utf16SequenceLength;
        }

        return IsCellAddress(name) || LooksLikeR1C1(name);
    }

    private static bool IsCellAddress(string name)
    {
        return CellRef.TryParse(name, out var address) && address.Row < Worksheet.MaxRows && address.Column < Worksheet.MaxColumns;
    }

    private static bool LooksLikeR1C1(string name)
    {
        var i = 0;

        if (i < name.Length && name[i] is 'R' or 'r')
        {
            i++;
            while (i < name.Length && char.IsAsciiDigit(name[i]))
            {
                i++;
            }
        }

        if (i < name.Length && name[i] is 'C' or 'c')
        {
            i++;
            while (i < name.Length && char.IsAsciiDigit(name[i]))
            {
                i++;
            }
        }

        return i > 0 && i == name.Length;
    }
}

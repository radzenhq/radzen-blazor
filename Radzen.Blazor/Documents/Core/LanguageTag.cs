using System;

namespace Radzen.Documents.Core;

internal static class LanguageTag
{
    // RFC 5646 2.1: langtag = language ["-" script] ["-" region] *("-" variant)
    // *("-" extension) ["-" privateuse], plus a private-use-only tag.
    internal static bool IsWellFormed(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var subtags = value.Split('-');
        foreach (var subtag in subtags)
        {
            if (subtag.Length is 0 or > 8 || !IsAlphanumeric(subtag))
            {
                return false;
            }
        }

        var index = 0;
        if (IsSingleton(subtags[0], 'x'))
        {
            return ReadPrivateUse(subtags, ref index) && index == subtags.Length;
        }

        if (!ReadLanguage(subtags, ref index))
        {
            return false;
        }

        if (index < subtags.Length && subtags[index].Length == 4 && IsAlpha(subtags[index]))
        {
            index++;
        }

        if (index < subtags.Length && IsRegion(subtags[index]))
        {
            index++;
        }

        while (index < subtags.Length && IsVariant(subtags[index]))
        {
            index++;
        }

        while (index < subtags.Length && IsExtensionSingleton(subtags[index]))
        {
            index++;
            var count = 0;
            while (index < subtags.Length && subtags[index].Length is >= 2 and <= 8)
            {
                index++;
                count++;
            }

            if (count == 0)
            {
                return false;
            }
        }

        if (index < subtags.Length && IsSingleton(subtags[index], 'x') && !ReadPrivateUse(subtags, ref index))
        {
            return false;
        }

        return index == subtags.Length;
    }

    internal static string? Validated(string? value, string subject)
    {
        if (value is null || IsWellFormed(value))
        {
            return value;
        }

        throw new ArgumentException(
            $"{subject} must be a well-formed BCP 47 (RFC 5646) language tag such as \"en\", \"en-US\" or \"sr-Latn-RS\".",
            nameof(value));
    }

    private static bool ReadLanguage(string[] subtags, ref int index)
    {
        var language = subtags[index];
        if (!IsAlpha(language))
        {
            return false;
        }

        if (language.Length is 2 or 3)
        {
            index++;
            // RFC 5646 2.1: extlang = 3ALPHA *2("-" 3ALPHA).
            for (var i = 0; i < 3 && index < subtags.Length; i++)
            {
                if (subtags[index].Length != 3 || !IsAlpha(subtags[index]))
                {
                    break;
                }

                index++;
            }

            return true;
        }

        if (language.Length is >= 4 and <= 8)
        {
            index++;
            return true;
        }

        return false;
    }

    private static bool ReadPrivateUse(string[] subtags, ref int index)
    {
        index++;
        var count = 0;
        while (index < subtags.Length)
        {
            index++;
            count++;
        }

        return count > 0;
    }

    private static bool IsRegion(string subtag)
        => (subtag.Length == 2 && IsAlpha(subtag)) || (subtag.Length == 3 && IsDigits(subtag));

    private static bool IsVariant(string subtag)
        => (subtag.Length is >= 5 and <= 8)
            || (subtag.Length == 4 && char.IsAsciiDigit(subtag[0]));

    private static bool IsExtensionSingleton(string subtag)
        => subtag.Length == 1 && !IsSingleton(subtag, 'x');

    private static bool IsSingleton(string subtag, char letter)
        => subtag.Length == 1 && char.ToLowerInvariant(subtag[0]) == letter;

    private static bool IsAlpha(string subtag)
    {
        foreach (var character in subtag)
        {
            if (!char.IsAsciiLetter(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDigits(string subtag)
    {
        foreach (var character in subtag)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlphanumeric(string subtag)
    {
        foreach (var character in subtag)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}

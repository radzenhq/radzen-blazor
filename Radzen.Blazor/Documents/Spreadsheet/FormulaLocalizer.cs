using System;
using System.Globalization;
using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Converts formulas between the canonical invariant form (stored in <see cref="Cell.Formula"/> and XLSX)
/// and the localized form users type in comma-decimal cultures (";" separators, "," decimals) -
/// the Excel FormulaLocal semantics. A pure text transform; the engine and storage stay invariant.
/// </summary>
internal static class FormulaLocalizer
{
    /// <summary>
    /// Converts a canonical invariant formula to the localized form for the specified culture.
    /// </summary>
    internal static string ToLocalized(string formula, CultureInfo culture)
    {
        if (!TryGetLocalSyntax(formula, culture, out var localDecimal, out var localSeparator))
        {
            return formula;
        }

        return Transform(formula, separator: ',', toSeparator: localSeparator, decimalChar: '.', toDecimal: localDecimal, lenientComma: false);
    }

    /// <summary>
    /// Converts a formula typed in localized form back to the canonical invariant form. Lenient:
    /// "," is also accepted as an argument separator where it cannot be a decimal (=SUM(A1,A2) works in de-DE).
    /// </summary>
    internal static string ToInvariant(string formula, CultureInfo culture)
    {
        if (!TryGetLocalSyntax(formula, culture, out var localDecimal, out var localSeparator))
        {
            return formula;
        }

        return Transform(formula, separator: localSeparator, toSeparator: ',', decimalChar: localDecimal, toDecimal: '.', lenientComma: true);
    }

    private static bool TryGetLocalSyntax(string formula, CultureInfo culture, out char localDecimal, out char localSeparator)
    {
        localDecimal = '.';
        localSeparator = ',';

        var dec = culture.NumberFormat.NumberDecimalSeparator;

        if (string.IsNullOrEmpty(formula) || dec.Length != 1 || dec[0] == '.')
        {
            return false;
        }

        localDecimal = dec[0];

        if (localDecimal == ',')
        {
            localSeparator = ';';
        }
        else
        {
            var list = culture.TextInfo.ListSeparator;
            localSeparator = list.Length == 1 && list[0] != localDecimal && list[0] != '.' ? list[0] : ';';
        }

        return true;
    }

    private static string Transform(string formula, char separator, char toSeparator, char decimalChar, char toDecimal, bool lenientComma)
    {
        // Allocated on the first converted character; an unchanged formula returns itself.
        StringBuilder? result = null;

        // The digits of A1 or my.name2 must not form a decimal context, so track how a run started.
        var inRun = false;
        var runStartsWithIdentifier = false;

        for (var i = 0; i < formula.Length; i++)
        {
            var c = formula[i];

            // Array constants are not supported by the engine.
            if (c is '{' or '}')
            {
                return formula;
            }

            if (c is '"' or '\'')
            {
                var end = SkipQuoted(formula, i, c);
                result?.Append(formula, i, end - i);
                i = end - 1;
                inRun = false;
                continue;
            }

            if (c == '[')
            {
                var end = SkipBrackets(formula, i);
                result?.Append(formula, i, end - i);
                i = end - 1;
                inRun = false;
                continue;
            }

            char? converted = null;

            if (c == decimalChar && IsDecimalContext(formula, i, inRun, runStartsWithIdentifier))
            {
                converted = toDecimal;
            }
            else if (c == separator || (lenientComma && c == ','))
            {
                converted = toSeparator;
            }

            if (converted is char replacement && replacement != c)
            {
                result ??= new StringBuilder(formula.Length).Append(formula, 0, i);
                result.Append(replacement);
                inRun = false;
                continue;
            }

            if (char.IsAsciiLetter(c) || c is '_' or '$')
            {
                if (!inRun)
                {
                    inRun = true;
                    runStartsWithIdentifier = true;
                }
            }
            else if (char.IsAsciiDigit(c))
            {
                if (!inRun)
                {
                    inRun = true;
                    runStartsWithIdentifier = false;
                }
            }
            else
            {
                inRun = false;
            }

            result?.Append(c);
        }

        return result?.ToString() ?? formula;
    }

    // Honors doubled-quote escapes ("" and '').
    private static int SkipQuoted(string formula, int start, char quote)
    {
        for (var i = start + 1; i < formula.Length; i++)
        {
            if (formula[i] == quote)
            {
                if (i + 1 < formula.Length && formula[i + 1] == quote)
                {
                    i++;
                    continue;
                }

                return i + 1;
            }
        }

        return formula.Length;
    }

    // Depth-counted: Table[[Col1],[Col2]] must skip to the outer close bracket.
    private static int SkipBrackets(string formula, int start)
    {
        var depth = 0;

        for (var i = start; i < formula.Length; i++)
        {
            if (formula[i] == '[')
            {
                depth++;
            }
            else if (formula[i] == ']' && --depth == 0)
            {
                return i + 1;
            }
        }

        return formula.Length;
    }

    // Decimal context: digit on the right, and a pure numeric run or formula boundary on the left.
    private static bool IsDecimalContext(string formula, int index, bool inRun, bool runStartsWithIdentifier)
    {
        if (index + 1 >= formula.Length || !char.IsAsciiDigit(formula[index + 1]))
        {
            return false;
        }

        if (index == 0)
        {
            return true;
        }

        var prev = formula[index - 1];

        if (char.IsAsciiDigit(prev))
        {
            return inRun && !runStartsWithIdentifier;
        }

        return prev is '=' or '(' or ',' or ';' or '+' or '-' or '*' or '/' or '^' or '<' or '>' or '&' or '%' or ':';
    }
}

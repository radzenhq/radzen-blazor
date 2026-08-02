using System;
using System.Globalization;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.3: integers are emitted without a decimal point; reals are emitted
// culture-invariantly with a dot, trailing zeros trimmed, never in exponent notation.
internal sealed class NumberObject : DocumentObject
{
    private readonly long integerValue;
    private readonly double realValue;

    public NumberObject(int value)
        : this((long)value)
    {
    }

    public NumberObject(long value)
    {
        integerValue = value;
        realValue = value;
        IsInteger = true;
    }

    public NumberObject(double value)
    {
        realValue = value;
        integerValue = (long)value;
        IsInteger = false;
    }

    public bool IsInteger { get; }

    public int IntValue => (int)integerValue;

    public double DoubleValue => IsInteger ? integerValue : realValue;

    // ISO 32000-1 7.3.3: PDF has no valid token for non-finite numbers.
    internal override void Write(Stream stream, WriteContext context)
    {
        if (!IsInteger && !double.IsFinite(realValue))
        {
            throw new InvalidOperationException("A PDF number cannot be NaN or infinite.");
        }

        if (IsInteger)
        {
            PdfBytes.WriteInteger(stream, integerValue);
        }
        else
        {
            PdfBytes.WriteAscii(stream, FormatReal(realValue));
        }
    }

    private static string FormatReal(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var eIndex = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is 'E' or 'e')
            {
                eIndex = i;
                break;
            }
        }

        return eIndex < 0 ? text : ExpandExponent(text, eIndex);
    }

    private static string ExpandExponent(string text, int eIndex)
    {
        var start = 0;
        var negative = text[0] == '-';
        if (negative)
        {
            start = 1;
        }

        var exponent = int.Parse(text.AsSpan(eIndex + 1), CultureInfo.InvariantCulture);

        var dot = -1;
        for (var i = start; i < eIndex; i++)
        {
            if (text[i] == '.')
            {
                dot = i;
                break;
            }
        }

        var digits = dot < 0
            ? text[start..eIndex]
            : text[start..dot] + text.Substring(dot + 1, eIndex - dot - 1);
        var integerLength = dot < 0 ? eIndex - start : dot - start;
        var pointPosition = integerLength + exponent;

        string result;
        if (pointPosition <= 0)
        {
            result = "0." + new string('0', -pointPosition) + digits;
        }
        else if (pointPosition >= digits.Length)
        {
            result = digits + new string('0', pointPosition - digits.Length);
        }
        else
        {
            result = string.Concat(digits.AsSpan(0, pointPosition), ".", digits.AsSpan(pointPosition));
        }

        if (result.Contains('.', StringComparison.Ordinal))
        {
            result = result.TrimEnd('0').TrimEnd('.');
        }

        return negative && result != "0" ? "-" + result : result;
    }
}

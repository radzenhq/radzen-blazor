using System;
using System.Globalization;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF numeric object (ISO 32000-1 section 7.3.3). Integers are emitted
/// without a decimal point; reals are emitted culture-invariantly with a dot,
/// trailing zeros trimmed, and never in exponent notation.
/// </summary>
public sealed class NumberObject : DocumentObject
{
    private readonly long integerValue;
    private readonly double realValue;

    /// <summary>
    /// Initializes a new integer-valued instance of the <see cref="NumberObject"/> class.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public NumberObject(int value)
        : this((long)value)
    {
    }

    /// <summary>
    /// Initializes a new integer-valued instance of the <see cref="NumberObject"/>
    /// class from a 64-bit value, preserving integers wider than 32 bits.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public NumberObject(long value)
    {
        integerValue = value;
        realValue = value;
        IsInteger = true;
    }

    /// <summary>
    /// Initializes a new real-valued instance of the <see cref="NumberObject"/> class.
    /// </summary>
    /// <param name="value">The real value.</param>
    public NumberObject(double value)
    {
        realValue = value;
        integerValue = (long)value;
        IsInteger = false;
    }

    /// <summary>
    /// Gets a value indicating whether this number was created as an integer.
    /// </summary>
    public bool IsInteger { get; }

    /// <summary>
    /// Gets the value as a 32-bit integer (truncated when the number is real).
    /// </summary>
    public int IntValue => (int)integerValue;

    /// <summary>
    /// Gets the value as a double.
    /// </summary>
    public double DoubleValue => IsInteger ? integerValue : realValue;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The value is NaN or infinite; PDF
    /// has no valid token for non-finite numbers (ISO 32000-1 section 7.3.3).</exception>
    public override void Write(Stream stream)
    {
        if (!IsInteger && !double.IsFinite(realValue))
        {
            throw new InvalidOperationException("A PDF number cannot be NaN or infinite.");
        }

        PdfBytes.WriteAscii(stream, IsInteger
            ? integerValue.ToString(CultureInfo.InvariantCulture)
            : FormatReal(realValue));
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

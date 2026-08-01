using System;
using System.Globalization;

namespace Radzen.Documents.Core;


/// <summary>
/// Represents a measurement, either absolute (expressed in typographic points, 1/72 inch) or
/// relative (expressed as a percentage of a reference length). Stored culture-invariantly.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    private const double PointsPerInch = 72.0;
    private const double PointsPerCentimeter = PointsPerInch / 2.54;
    private const double PointsPerMillimeter = PointsPerInch / 25.4;

    private readonly double value;
    private readonly bool relative;

    private Unit(double value, bool relative)
    {
        this.value = value;
        this.relative = relative;
    }

    private static double Finite(double value, string parameterName)
        => double.IsFinite(value)
            ? value
            : throw new ArgumentException("A measurement must be a finite number.", parameterName);

    /// <summary>
    /// Gets the measurement in points.
    /// </summary>
    /// <exception cref="InvalidOperationException">The measurement is relative. Call <see cref="Resolve"/> instead.</exception>
    public double Point => relative
        ? throw new InvalidOperationException("A relative measurement has no point value until it is resolved against a reference length.")
        : value;

    /// <summary>
    /// Gets a value indicating whether the measurement is a percentage of a reference length
    /// rather than an absolute length.
    /// </summary>
    public bool IsRelative => relative;

    /// <summary>
    /// Gets the measurement as a percentage of its reference length.
    /// </summary>
    /// <exception cref="InvalidOperationException">The measurement is absolute.</exception>
    public double Percent => relative
        ? value
        : throw new InvalidOperationException("An absolute measurement has no percentage value.");

    /// <summary>
    /// Resolves the measurement against <paramref name="reference"/>. An absolute measurement
    /// resolves to its own point value and ignores the reference; a relative one resolves to
    /// the corresponding fraction of the reference length.
    /// </summary>
    /// <param name="reference">The reference length, in points.</param>
    /// <returns>The resolved length in points.</returns>
    public double Resolve(double reference) => relative ? value / 100.0 * reference : value;

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in points.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    public static Unit FromPoint(double value) => new(Finite(value, nameof(value)), false);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a percentage of a reference length.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    public static Unit FromPercent(double value) => new(Finite(value, nameof(value)), true);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in inches (1 inch = 72 points).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    public static Unit FromInch(double value) => new(Finite(value, nameof(value)) * PointsPerInch, false);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in centimeters.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    public static Unit FromCentimeter(double value) => new(Finite(value, nameof(value)) * PointsPerCentimeter, false);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in millimeters.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    public static Unit FromMillimeter(double value) => new(Finite(value, nameof(value)) * PointsPerMillimeter, false);

    /// <summary>
    /// Converts a <see cref="double"/> value, interpreted as points, to a <see cref="Unit"/>.
    /// The conversion throws rather than producing a measurement that cannot be laid out, so a
    /// value that may not be finite must be checked before it is assigned.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not finite.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "CA2225:Operator overloads have named alternates",
        Justification = "FromPoint is the named alternate; the operand is a point value, not an unqualified double.")]
    public static implicit operator Unit(double value) => new(Finite(value, nameof(value)), false);

    /// <summary>
    /// Parses a measurement such as "9cm", "5mm", "1in", "12pt", "50%" or a bare number
    /// (interpreted as points). Parsing is culture-invariant.
    /// </summary>
    /// <param name="value">The measurement text.</param>
    /// <returns>The parsed <see cref="Unit"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is not a valid measurement.</exception>
    public static Unit Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var text = value.Trim();
        var number = text;
        var factor = 1.0;
        if (text.Length >= 2 && text[^1] == '%')
        {
            if (!double.TryParse(text[..^1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent)
                || !double.IsFinite(percent))
            {
                throw new FormatException($"'{value}' is not a valid measurement.");
            }

            return new(percent, true);
        }

        if (text.Length >= 2)
        {
            var scale = text[^2..].ToLowerInvariant() switch
            {
                "cm" => PointsPerCentimeter,
                "mm" => PointsPerMillimeter,
                "in" => PointsPerInch,
                "pt" => 1.0,
                _ => (double?)null,
            };

            if (scale is { } resolved)
            {
                factor = resolved;
                number = text[..^2].Trim();
            }
        }

        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !double.IsFinite(parsed))
        {
            throw new FormatException($"'{value}' is not a valid measurement.");
        }

        return new(parsed * factor, false);
    }

    /// <summary>
    /// Tries to parse a measurement such as "9cm", "5mm", "1in", "12pt", "50%" or a bare number
    /// (interpreted as points). Parsing is culture-invariant.
    /// </summary>
    /// <param name="value">The measurement text.</param>
    /// <param name="result">The parsed measurement, or the default when parsing fails.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is a valid measurement.</returns>
    public static bool TryParse(string? value, out Unit result)
    {
        if (value is not null)
        {
            try
            {
                result = Parse(value);
                return true;
            }
            catch (FormatException)
            {
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Adds two measurements. Both must be absolute, or both relative.
    /// </summary>
    public static Unit operator +(Unit left, Unit right) => Combine(left, right, left.value + right.value);

    /// <summary>
    /// Subtracts one measurement from another. Both must be absolute, or both relative.
    /// </summary>
    public static Unit operator -(Unit left, Unit right) => Combine(left, right, left.value - right.value);

    private static Unit Combine(Unit left, Unit right, double combined) => left.relative == right.relative
        ? new(combined, left.relative)
        : throw new InvalidOperationException("An absolute measurement cannot be combined with a relative one.");

    /// <summary>
    /// Adds two measurements.
    /// </summary>
    public static Unit Add(Unit left, Unit right) => left + right;

    /// <summary>
    /// Subtracts one measurement from another.
    /// </summary>
    public static Unit Subtract(Unit left, Unit right) => left - right;

    /// <summary>
    /// Determines whether two measurements are equal.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => left.Equals(right);

    /// <summary>
    /// Determines whether two measurements are not equal.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => !left.Equals(right);

    /// <summary>
    /// Determines whether the left measurement is smaller than the right one. Both must be absolute,
    /// or both relative.
    /// </summary>
    /// <exception cref="InvalidOperationException">One measurement is absolute and the other relative.</exception>
    public static bool operator <(Unit left, Unit right) => left.CompareTo(right) < 0;

    /// <summary>
    /// Determines whether the left measurement is greater than the right one. Both must be absolute,
    /// or both relative.
    /// </summary>
    /// <exception cref="InvalidOperationException">One measurement is absolute and the other relative.</exception>
    public static bool operator >(Unit left, Unit right) => left.CompareTo(right) > 0;

    /// <summary>
    /// Determines whether the left measurement is smaller than or equal to the right one. Both must be
    /// absolute, or both relative.
    /// </summary>
    /// <exception cref="InvalidOperationException">One measurement is absolute and the other relative.</exception>
    public static bool operator <=(Unit left, Unit right) => left.CompareTo(right) <= 0;

    /// <summary>
    /// Determines whether the left measurement is greater than or equal to the right one. Both must be
    /// absolute, or both relative.
    /// </summary>
    /// <exception cref="InvalidOperationException">One measurement is absolute and the other relative.</exception>
    public static bool operator >=(Unit left, Unit right) => left.CompareTo(right) >= 0;

    /// <inheritdoc/>
    public bool Equals(Unit other) => relative == other.relative && value.Equals(other.value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Unit other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(value, relative);

    /// <summary>
    /// Compares this measurement with another of the same kind. Both must be absolute, or both relative.
    /// </summary>
    /// <exception cref="InvalidOperationException">One measurement is absolute and the other relative.</exception>
    public int CompareTo(Unit other) => relative == other.relative
        ? value.CompareTo(other.value)
        : throw new InvalidOperationException("An absolute measurement cannot be compared with a relative one.");

    /// <summary>
    /// Returns the measurement as text <see cref="Parse"/> reads back unchanged: the number in the
    /// shortest form that round-trips, culture-invariantly, followed by <c>pt</c> for an absolute
    /// measurement or <c>%</c> for a relative one.
    /// </summary>
    public override string ToString()
        => value.ToString(CultureInfo.InvariantCulture) + (relative ? "%" : "pt");

    /// <inheritdoc/>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Unit other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Unit)}.", nameof(obj)),
    };
}

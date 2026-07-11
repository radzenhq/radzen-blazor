using System;
using System.Globalization;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// Represents a measurement expressed in typographic points (1/72 inch). Stored culture-invariantly.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    private const double PointsPerInch = 72.0;
    private const double PointsPerCentimeter = PointsPerInch / 2.54;
    private const double PointsPerMillimeter = PointsPerInch / 25.4;

    private Unit(double point) => Point = point;

    /// <summary>
    /// Gets the measurement in points.
    /// </summary>
    public double Point { get; }

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in points.
    /// </summary>
    public static Unit FromPoint(double value) => new(value);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in inches (1 inch = 72 points).
    /// </summary>
    public static Unit FromInch(double value) => new(value * PointsPerInch);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in centimeters.
    /// </summary>
    public static Unit FromCentimeter(double value) => new(value * PointsPerCentimeter);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a value in millimeters.
    /// </summary>
    public static Unit FromMillimeter(double value) => new(value * PointsPerMillimeter);

    /// <summary>
    /// Converts a <see cref="double"/> value, interpreted as points, to a <see cref="Unit"/>.
    /// </summary>
    public static implicit operator Unit(double value) => new(value);

    /// <summary>
    /// Parses a measurement such as "9cm", "5mm", "1in", "12pt" or a bare number (interpreted as points,
    /// matching MigraDoc). Parsing is culture-invariant.
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

        if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new FormatException($"'{value}' is not a valid measurement.");
        }

        return new(parsed * factor);
    }

    /// <summary>
    /// Converts a measurement string (see <see cref="Parse"/>) to a <see cref="Unit"/>.
    /// </summary>
    public static implicit operator Unit(string value) => Parse(value);

    /// <summary>
    /// Creates a <see cref="Unit"/> from a measurement string (see <see cref="Parse"/>).
    /// </summary>
    public static Unit FromString(string value) => Parse(value);

    /// <summary>
    /// Converts a <see cref="double"/> value, interpreted as points, to a <see cref="Unit"/>.
    /// </summary>
    public static Unit FromDouble(double value) => new(value);

    /// <summary>
    /// Adds two measurements.
    /// </summary>
    public static Unit operator +(Unit left, Unit right) => new(left.Point + right.Point);

    /// <summary>
    /// Subtracts one measurement from another.
    /// </summary>
    public static Unit operator -(Unit left, Unit right) => new(left.Point - right.Point);

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
    /// Determines whether the left measurement is smaller than the right one.
    /// </summary>
    public static bool operator <(Unit left, Unit right) => left.Point < right.Point;

    /// <summary>
    /// Determines whether the left measurement is greater than the right one.
    /// </summary>
    public static bool operator >(Unit left, Unit right) => left.Point > right.Point;

    /// <summary>
    /// Determines whether the left measurement is smaller than or equal to the right one.
    /// </summary>
    public static bool operator <=(Unit left, Unit right) => left.Point <= right.Point;

    /// <summary>
    /// Determines whether the left measurement is greater than or equal to the right one.
    /// </summary>
    public static bool operator >=(Unit left, Unit right) => left.Point >= right.Point;

    /// <inheritdoc/>
    public bool Equals(Unit other) => Point.Equals(other.Point);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Unit other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Point.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(Unit other) => Point.CompareTo(other.Point);

    /// <inheritdoc/>
    public int CompareTo(object? obj) => obj switch
    {
        null => 1,
        Unit other => CompareTo(other),
        _ => throw new ArgumentException($"Object must be of type {nameof(Unit)}.", nameof(obj)),
    };
}

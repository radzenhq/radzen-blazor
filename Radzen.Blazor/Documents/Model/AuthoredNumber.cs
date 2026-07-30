using System;

namespace Radzen.Documents;

internal static class AuthoredNumber
{
    internal static double Finite(double value, string subject)
        => double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"{subject} must be a finite number.");

    internal static double Positive(double value, string subject)
        => double.IsFinite(value) && value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"{subject} must be a finite number greater than zero.");

    internal static int NonNegative(int value, string subject)
        => value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"{subject} cannot be negative.");
}

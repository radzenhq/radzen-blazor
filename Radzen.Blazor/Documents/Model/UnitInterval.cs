using System;

namespace Radzen.Documents;

internal static class UnitInterval
{
    internal static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    internal static double ValidatedOpacity(double value, string subject)
    {
        if (!double.IsFinite(value) || value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"{subject} opacity must be between 0 and 1.");
        }

        return value;
    }
}

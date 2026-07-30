using System;
using System.Globalization;

namespace Radzen.Documents.Pdf;

internal static class ExtGStateRegistration
{
    public static string RegisterAlpha<TValue>(
        ResourceNameAllocator<string, TValue> states,
        double fillAlpha,
        double strokeAlpha,
        BlendMode? blend,
        Func<string, TValue> create)
    {
        if (double.IsNaN(fillAlpha) || double.IsNaN(strokeAlpha))
        {
            return states.Add(create);
        }

        return states.GetOrAdd(AlphaKey(fillAlpha, strokeAlpha, blend), create);
    }

    private static string AlphaKey(double fillAlpha, double strokeAlpha, BlendMode? blend) => string.Create(
        CultureInfo.InvariantCulture,
        $"a|{Normalize(fillAlpha)}|{Normalize(strokeAlpha)}|{blend}");

    private static double Normalize(double alpha) => Math.Clamp(alpha, 0, 1) + 0.0;
}

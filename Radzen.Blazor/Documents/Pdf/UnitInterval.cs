namespace Radzen.Documents.Pdf;

internal static class UnitInterval
{
    internal static double Clamp(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}

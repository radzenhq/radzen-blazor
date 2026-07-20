namespace Radzen.Documents.Pdf.Fonts;

internal static class FontMetric
{
    public static double Scale(double designUnits, double size, double unitsPerEm)
        => designUnits * size / unitsPerEm;

    public static int PairKey(int left, int right) => (left << 16) | right;
}

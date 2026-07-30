namespace Radzen.Documents.Fonts;

internal static class FontMetric
{
    // Adobe Font Metrics File Format Specification 4.1: AFM character widths are expressed in a
    // 1000-unit glyph space, independent of any sfnt head.unitsPerEm.
    public const double AfmDesignUnitsPerEm = 1000;

    public static double Scale(double designUnits, double size, double unitsPerEm)
        => designUnits * size / unitsPerEm;

    public static double ScaleAfm(double designUnits, double size)
        => Scale(designUnits, size, AfmDesignUnitsPerEm);

    public static int PairKey(int left, int right) => (left << 16) | right;
}

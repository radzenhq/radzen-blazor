namespace Radzen.Documents.Fonts;

internal enum BuiltInFontFamily
{
    Sans,
    Serif,
    Monospace,
    Symbol,
    ZapfDingbats,
}

internal readonly record struct BuiltInFaceMetrics(
    double DesignUnitsPerEm,
    double Ascender,
    double Descender,
    double CapHeight,
    double XHeight,
    double ItalicAngle,
    double UnderlinePosition,
    double UnderlineThickness,
    bool IsFixedPitch,
    double BBoxLeft,
    double BBoxBottom,
    double BBoxRight,
    double BBoxTop);

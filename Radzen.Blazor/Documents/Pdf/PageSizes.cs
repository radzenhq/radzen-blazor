namespace Radzen.Documents.Pdf;


/// <summary>
/// Predefined page sizes. ISO A sizes are derived from their millimeter dimensions; US sizes are exact points.
/// </summary>
public static class PageSizes
{
    private static PageSize FromMillimeters(double width, double height)
        => new(Unit.FromMillimeter(width), Unit.FromMillimeter(height));

    private static PageSize FromPoints(double width, double height)
        => new(Unit.FromPoint(width), Unit.FromPoint(height));

    /// <summary>A0 (841 x 1189 mm).</summary>
    public static PageSize A0 => FromMillimeters(841, 1189);

    /// <summary>A1 (594 x 841 mm).</summary>
    public static PageSize A1 => FromMillimeters(594, 841);

    /// <summary>A2 (420 x 594 mm).</summary>
    public static PageSize A2 => FromMillimeters(420, 594);

    /// <summary>A3 (297 x 420 mm).</summary>
    public static PageSize A3 => FromMillimeters(297, 420);

    /// <summary>A4 (210 x 297 mm).</summary>
    public static PageSize A4 => FromMillimeters(210, 297);

    /// <summary>A5 (148 x 210 mm).</summary>
    public static PageSize A5 => FromMillimeters(148, 210);

    /// <summary>A6 (105 x 148 mm).</summary>
    public static PageSize A6 => FromMillimeters(105, 148);

    /// <summary>US Letter (612 x 792 pt).</summary>
    public static PageSize Letter => FromPoints(612, 792);

    /// <summary>US Legal (612 x 1008 pt).</summary>
    public static PageSize Legal => FromPoints(612, 1008);

    /// <summary>US Tabloid (792 x 1224 pt).</summary>
    public static PageSize Tabloid => FromPoints(792, 1224);
}

using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal enum DeviceColorKind
{
    Cmyk,
    Named,
    Gray,
}

internal readonly record struct DeviceColor(DeviceColorKind Kind, string? ColorSpace, double[] Operands, string? PatternName = null)
{
    public static DeviceColor Gray(double gray)
        => new(DeviceColorKind.Gray, null, [UnitInterval.Clamp(gray)]);

    public static DeviceColor Cmyk(double cyan, double magenta, double yellow, double black)
        => new(DeviceColorKind.Cmyk, null,
            [UnitInterval.Clamp(cyan), UnitInterval.Clamp(magenta), UnitInterval.Clamp(yellow), UnitInterval.Clamp(black)]);
}

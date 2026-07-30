using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf;

internal static class ContentClone
{
    public static DeviceColor? CopyDeviceColor(DeviceColor? source)
        => source is { } color ? color with { Operands = [.. color.Operands] } : null;

    public static Font CopyFont(Font source) => new()
    {
        Family = source.Family,
        Size = source.Size,
        Bold = source.Bold,
        Italic = source.Italic,
        Underline = source.Underline,
        Strikethrough = source.Strikethrough,
        Color = source.Color,
    };
}

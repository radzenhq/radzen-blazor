using Radzen.Documents;

namespace Radzen.Blazor;

/// <summary>
/// Error correction.
/// </summary>
public enum RadzenQREcc
{
    /// <summary>
    /// 7% recovery.
    /// </summary>
    Low = (int)QrErrorCorrection.Low,
    /// <summary>
    /// 15% recovery.
    /// </summary>
    Medium = (int)QrErrorCorrection.Medium,
    /// <summary>
    /// 25% recovery.
    /// </summary>
    Quartile = (int)QrErrorCorrection.Quartile,
    /// <summary>
    /// 30% recovery.
    /// </summary>
    High = (int)QrErrorCorrection.High
}

/// <summary>
/// Provides QR encoding utilities for UTF-8 strings and raw bytes.
/// </summary>
public static class RadzenQREncoder
{
    /// <summary>Encode a UTF-8 string into a QR module matrix.</summary>
    public static bool[,] EncodeUtf8(string value, RadzenQREcc ecc, int minVersion = 1, int maxVersion = 40)
        => QrEncoder.EncodeUtf8(value, (QrErrorCorrection)(int)ecc, minVersion, maxVersion);

    /// <summary>Encode raw bytes into a QR module matrix.</summary>
    public static bool[,] EncodeBytes(byte[] data, RadzenQREcc ecc = RadzenQREcc.Medium, int minVersion = 1, int maxVersion = 40)
        => QrEncoder.EncodeBytes(data, (QrErrorCorrection)(int)ecc, minVersion, maxVersion);

    /// <summary>Render a module matrix into an SVG string with a 4-module quiet zone.</summary>
    public static string ToSvg(
        bool[,] modules,
        int moduleSize = 8,
        string foreground = "#000000",
        string background = "#FFFFFF",
        QRCodeModuleShape moduleShape = QRCodeModuleShape.Square,
        QRCodeEyeShape eyeShape = QRCodeEyeShape.Square,
        QRCodeEyeShape? eyeShapeTopLeft = null,
        QRCodeEyeShape? eyeShapeTopRight = null,
        QRCodeEyeShape? eyeShapeBottomLeft = null,
        string? eyeColor = null,
        string? eyeColorTopLeft = null,
        string? eyeColorTopRight = null,
        string? eyeColorBottomLeft = null,
        string? image = null,
        string imageBackground = "#FFF")
        => QrEncoder.ToSvg(
            modules,
            moduleSize,
            foreground,
            background,
            (QrModuleShape)(int)moduleShape,
            (QrEyeShape)(int)eyeShape,
            ToNeutral(eyeShapeTopLeft),
            ToNeutral(eyeShapeTopRight),
            ToNeutral(eyeShapeBottomLeft),
            eyeColor,
            eyeColorTopLeft,
            eyeColorTopRight,
            eyeColorBottomLeft,
            image,
            imageBackground);

    private static QrEyeShape? ToNeutral(QRCodeEyeShape? shape)
        => shape.HasValue ? (QrEyeShape)(int)shape.Value : null;
}

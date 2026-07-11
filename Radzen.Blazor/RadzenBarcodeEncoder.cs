using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;

namespace Radzen.Blazor;

/// <summary>
/// Represents a rectangle used for barcode rendering, with position and size.
/// </summary>
public readonly struct BarcodeRect(double x, double y, double width, double height)
{
    /// <summary>
    /// The X position of the rectangle.
    /// </summary>
    public readonly double X = x;
    /// <summary>
    /// The Y position of the rectangle.
    /// </summary>
    public readonly double Y = y;
    /// <summary>
    /// The width of the rectangle.
    /// </summary>
    public readonly double Width = width;
    /// <summary>
    /// The height of the rectangle.
    /// </summary>
    public readonly double Height = height;

    /// <summary>
    /// Converts this rectangle to a <see cref="Radzen.Documents.BarcodeRect"/>.
    /// </summary>
    /// <param name="rect">The rectangle to convert.</param>
    public static implicit operator Radzen.Documents.BarcodeRect(BarcodeRect rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>
    /// Converts a <see cref="Radzen.Documents.BarcodeRect"/> to this rectangle type.
    /// </summary>
    /// <param name="rect">The rectangle to convert.</param>
    public static implicit operator BarcodeRect(Radzen.Documents.BarcodeRect rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>
    /// Converts this rectangle to a <see cref="Radzen.Documents.BarcodeRect"/>.
    /// </summary>
    public Radzen.Documents.BarcodeRect ToBarcodeRect() => new(X, Y, Width, Height);

    /// <summary>
    /// Converts a <see cref="Radzen.Documents.BarcodeRect"/> to this rectangle type.
    /// </summary>
    /// <param name="rect">The rectangle to convert.</param>
    public static BarcodeRect FromBarcodeRect(Radzen.Documents.BarcodeRect rect)
        => new(rect.X, rect.Y, rect.Width, rect.Height);
}

/// <summary>
/// Provides 1D barcode encoding utilities for common symbologies.
/// </summary>
public static class RadzenBarcodeEncoder
{
    /// <summary>
    /// Encodes a string into Code 128 subset B module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode128B(string value) => BarcodeEncoder.EncodeCode128B(value);

    /// <summary>
    /// Encodes a string into Code 128 subset B module widths and returns the checksum.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksum">The calculated checksum value.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode128B(string value, out int checksum)
        => BarcodeEncoder.EncodeCode128B(value, out checksum);

    /// <summary>
    /// Encodes a string into Code 39 module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCode39(string value) => BarcodeEncoder.EncodeCode39(value);

    /// <summary>
    /// Encodes a string into ITF (Interleaved 2 of 5) module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeItf(string value) => BarcodeEncoder.EncodeItf(value);

    /// <summary>
    /// Encodes a string into Codabar module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeCodabar(string value) => BarcodeEncoder.EncodeCodabar(value);

    /// <summary>
    /// Encodes a string into EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeEan13(string value, out string checksumText)
        => BarcodeEncoder.EncodeEan13(value, out checksumText);

    /// <summary>
    /// Encodes a string into UPC-A bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeUpcA(string value, out string checksumText)
        => BarcodeEncoder.EncodeUpcA(value, out checksumText);

    /// <summary>
    /// Encodes a string into EAN-8 bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeEan8(string value, out string checksumText)
        => BarcodeEncoder.EncodeEan8(value, out checksumText);

    /// <summary>
    /// Encodes an ISBN as EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The ISBN value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeIsbnAsEan13(string value, out string checksumText)
        => BarcodeEncoder.EncodeIsbnAsEan13(value, out checksumText);

    /// <summary>
    /// Encodes an ISSN as EAN-13 bit pattern.
    /// </summary>
    /// <param name="value">The ISSN value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeIssnAsEan13(string value, out string checksumText)
        => BarcodeEncoder.EncodeIssnAsEan13(value, out checksumText);

    /// <summary>
    /// Encodes a Pharmacode value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The Pharmacode numeric value.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodePharmacode(string value, double barHeight, int quietZone)
    {
        var (bars, vbWidth) = BarcodeEncoder.EncodePharmacode(value, barHeight, quietZone);
        return (ToLegacy(bars), vbWidth);
    }

    /// <summary>
    /// Encodes a POSTNET value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodePostnet(string value, double barHeight, int quietZone, out string checksumText)
    {
        var (bars, vbWidth) = BarcodeEncoder.EncodePostnet(value, barHeight, quietZone, out checksumText);
        return (ToLegacy(bars), vbWidth);
    }

    /// <summary>
    /// Encodes a RM4SCC value and returns the bar geometry.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZone">The quiet zone in modules.</param>
    /// <param name="checksumText">The calculated checksum character.</param>
    /// <returns>The bar rectangles and viewBox width.</returns>
    public static (IReadOnlyList<BarcodeRect> bars, double vbWidth) EncodeRm4scc(string value, double barHeight, int quietZone, out string checksumText)
    {
        var (bars, vbWidth) = BarcodeEncoder.EncodeRm4scc(value, barHeight, quietZone, out checksumText);
        return (ToLegacy(bars), vbWidth);
    }

    /// <summary>
    /// Encodes a value into MSI (Modified Plessey) bit pattern.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum digit.</param>
    /// <returns>The bit pattern (1=bar, 0=space).</returns>
    public static string EncodeMsiPlessey(string value, out string checksumText)
        => BarcodeEncoder.EncodeMsiPlessey(value, out checksumText);

    /// <summary>
    /// Encodes a string into Telepen module widths.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="checksumText">The calculated checksum value.</param>
    /// <returns>The module widths (bar/space alternating, starting with bar).</returns>
    public static IReadOnlyList<int> EncodeTelepen(string value, out string checksumText)
        => BarcodeEncoder.EncodeTelepen(value, out checksumText);

    /// <summary>
    /// Encodes a barcode value and renders it into an SVG string.
    /// </summary>
    /// <param name="type">The barcode type.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="barHeight">The bar height in SVG units.</param>
    /// <param name="quietZoneModules">The quiet zone in modules.</param>
    /// <param name="foreground">The bar color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>An SVG string representing the barcode.</returns>
    public static string ToSvg(RadzenBarcodeType type, string value, double barHeight = 50, int quietZoneModules = 10, string foreground = "#000000", string background = "#FFFFFF")
        => BarcodeEncoder.ToSvg((BarcodeType)(int)type, value, barHeight, quietZoneModules, foreground, background);

    private static IReadOnlyList<BarcodeRect> ToLegacy(IReadOnlyList<Radzen.Documents.BarcodeRect> bars)
        => bars.Select(x => (BarcodeRect)x).ToList();
}

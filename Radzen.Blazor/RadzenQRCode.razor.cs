using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Radzen.Blazor
{
    /// <summary>
    /// QR code module shape.
    /// </summary>
    public enum QRCodeModuleShape
    {
        /// <summary>
        /// Square.
        /// </summary>
        Square,
        /// <summary>
        /// Rounded.
        /// </summary>
        Rounded,
        /// <summary>
        /// Circle.
        /// </summary>
        Circle
    }

    /// <summary>
    /// QR code eye shape.
    /// </summary>
    public enum QRCodeEyeShape
    {
        /// <summary>
        /// Square.
        /// </summary>
        Square,
        /// <summary>
        /// Rounded.
        /// </summary>
        Rounded,
        /// <summary>
        /// Circle.
        /// </summary>
        Framed
    }

    /// <summary>
    /// A QR code generator component that creates scannable QR codes from text, URLs, or other data as SVG graphics.
    /// RadzenQRCode supports extensive customization including colors, shapes, error correction, and embedded logos.
    /// Encodes data in a two-dimensional barcode format scannable by smartphones and QR code readers. Generates QR codes entirely client-side as SVG, with no external dependencies.
    /// Features data encoding (text, URLs, contact info, WiFi credentials, or any string data), error correction levels (Low, Medium, Quartile, High) for different damage resistance,
    /// customization of colors for modules (dots) and background with custom shapes (Square, Rounded, Circle), optional logo/image embedding in the center of the QR code,
    /// eye styling to customize the appearance of the three corner finder patterns, and responsive size (percentage or pixel-based) for responsive layouts.
    /// Higher error correction levels allow the QR code to remain scannable even when partially damaged or obscured (e.g., by a logo). Use Quartile or High error correction when embedding logos.
    /// </summary>
    /// <example>
    /// Basic QR code for URL:
    /// <code>
    /// &lt;RadzenQRCode Value="https://radzen.com" Size="200px" /&gt;
    /// </code>
    /// Customized QR code with logo:
    /// <code>
    /// &lt;RadzenQRCode Value="https://example.com" Foreground="#4169E1" Background="#F0F0F0"
    ///               Image="logo.png" ImageSizePercent="25" Ecc="RadzenQREcc.High"
    ///               ModuleShape="QRCodeModuleShape.Rounded" Size="300px" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenQRCode : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the error correction level determining how much damage the QR code can sustain while remaining scannable.
        /// Higher levels add more redundancy but reduce data capacity. Use High or Quartile when embedding logos.
        /// </summary>
        /// <value>The error correction level. Default is <see cref="RadzenQREcc.Quartile"/>.</value>
        [Parameter] public RadzenQREcc Ecc { get; set; } = RadzenQREcc.Quartile;

        /// <summary>
        /// Gets or sets the text or data to encode in the QR code.
        /// Can be plain text, URLs, contact information (vCard), WiFi credentials, or any string data.
        /// </summary>
        /// <value>The data to encode. Default is empty string.</value>
        [Parameter] public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the rendered size (both width and height) of the QR code SVG.
        /// Accepts CSS units (e.g., "200px", "100%", "10rem"). Use percentage for responsive sizing.
        /// </summary>
        /// <value>The size in CSS units. Default is "100%".</value>
        [Parameter] public string Size { get; set; } = "100%";

		/// <summary>
		/// Gets or sets the color of the QR code modules (the dark squares/dots).
		/// Supports any valid CSS color. Use high contrast with background for best scanability.
		/// </summary>
		/// <value>The foreground color. Default is "#000" (black).</value>
		[Parameter] public string Foreground { get; set; } = "#000";

		/// <summary>
		/// Gets or sets the background color of the QR code.
		/// Should contrast well with the foreground color for reliable scanning.
		/// </summary>
		/// <value>The background color. Default is "#FFF" (white).</value>
		[Parameter] public string Background { get; set; } = "#FFF";

        /// <summary>
        /// Gets or sets the visual shape of the QR code modules (data squares).
        /// Square creates standard QR codes, Rounded creates softer corners, Circle creates dot-based codes.
        /// </summary>
        /// <value>The module shape. Default is <see cref="QRCodeModuleShape.Square"/>.</value>
        [Parameter] public QRCodeModuleShape ModuleShape { get; set; } = QRCodeModuleShape.Square;

        /// <summary>Shape for finder eyes (the 3 corner boxes).</summary>
        [Parameter] public QRCodeEyeShape EyeShape { get; set; } = QRCodeEyeShape.Square;

        /// <summary>Shape for top left finder eye.</summary>
        [Parameter] public QRCodeEyeShape? EyeShapeTopLeft { get; set; }

        /// <summary>Shape for top right finder eye.</summary>
        [Parameter] public QRCodeEyeShape? EyeShapeTopRight { get; set; }

        /// <summary>Shape for top bottom finder eye.</summary>
        [Parameter] public QRCodeEyeShape? EyeShapeBottomLeft { get; set; }

        /// <summary>Optional color for eyes; if empty, falls back to Foreground.</summary>
        [Parameter] public string EyeColor { get; set; }

        /// <summary>Optional color for top left eye; if empty, falls back to EyeColor.</summary>
        [Parameter] public string EyeColorTopLeft { get; set; }

        /// <summary>Optional color for top right eye; if empty, falls back to EyeColor.</summary>
        [Parameter] public string EyeColorTopRight { get; set; }

        /// <summary>Optional color for bottom right eye; if empty, falls back to EyeColor.</summary>
        [Parameter] public string EyeColorBottomLeft { get; set; }

        /// <summary>URL, data: URI, or raw base64 (will be prefixed) to render in the center.</summary>
        [Parameter] public string Image { get; set; }

        /// <summary>Logo box size as % of the inner QR (without quiet zone). Safe range 5�60%. Default 20.</summary>
        [Parameter] public double ImageSizePercent { get; set; } = 20;

        /// <summary>Extra white padding around the logo in module units. Default 1.</summary>
        [Parameter] public double ImagePaddingModules { get; set; } = 1.0;

        /// <summary>Rounded-corner radius for the logo cutout in module units. Default 0.75.</summary>
        [Parameter] public double ImageCornerRadius { get; set; } = 0.75;

        /// <summary>Background color under the logo (usually white).</summary>
        [Parameter] public string ImageBackground { get; set; } = "#FFF";

        /// <summary>Background opacity under the logo (0..1). Default 1.</summary>
        [Parameter] public double ImageBackgroundOpacity { get; set; } = 1.0;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        protected override string GetComponentCssClass() => "rz-qrcode";
        private static string Format(double v) => v.ToString(CultureInfo.InvariantCulture);
        private static bool IsFinderCell(int r, int c, int n)
        {
            bool inTL = r < 7 && c < 7;
            bool inTR = r < 7 && c >= n - 7;
            bool inBL = r >= n - 7 && c < 7;
            return inTL || inTR || inBL;
        }
    }
}



using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
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
    /// QR code component base which generates a QR code module matrix and renders it as SVG.
    /// </summary>
    public partial class RadzenQRCode : RadzenComponent
	{
        /// <summary>
        /// Gets or sets the error correction .
        /// </summary>
        [Parameter] public RadzenQREcc Ecc { get; set; } = RadzenQREcc.Quartile;

        /// <summary>
        /// Gets or sets the text value to encode into the QR code.
        /// </summary>
        [Parameter] public string Value { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the rendered size (both width and height) in pixels of the SVG.
		/// </summary>
		[Parameter] public string Size { get; set; } = "100%";

		/// <summary>
		/// Gets or sets the foreground color used for the dark modules.
		/// </summary>
		[Parameter] public string Foreground { get; set; } = "#000";

		/// <summary>
		/// Gets or sets the background color of the QR code.
		/// </summary>
		[Parameter] public string Background { get; set; } = "#FFF";

        /// <summary>
        /// Gets or sets the edge shape.
        /// </summary>
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

        /// <summary>Logo box size as % of the inner QR (without quiet zone). Safe range 5–60%. Default 20.</summary>
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
    }
}



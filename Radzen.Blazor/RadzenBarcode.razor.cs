using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Barcode type.
    /// </summary>
    public enum RadzenBarcodeType
    {
        /// <summary>
        /// Code 128 (subset B). Encodes ASCII text and is widely supported.
        /// </summary>
        Code128,

        /// <summary>
        /// Code 39. Supports uppercase letters, digits, and a limited set of symbols.
        /// </summary>
        Code39,

        /// <summary>EAN-13 (GTIN-13).</summary>
        Ean13,

        /// <summary>EAN-8 (GTIN-8).</summary>
        Ean8,

        /// <summary>UPC-A.</summary>
        UpcA,

        /// <summary>ITF (Interleaved 2 of 5).</summary>
        Itf,

        /// <summary>POSTNET (USPS). 4/5-state style using short/long bars.</summary>
        Postnet,

        /// <summary>RM4SCC (Royal Mail 4-state customer code).</summary>
        Rm4scc,

        /// <summary>Codabar.</summary>
        Codabar,

        /// <summary>Pharmacode (one-track).</summary>
        Pharmacode,

        /// <summary>ISBN (encodes as EAN-13 with 978/979 prefix).</summary>
        Isbn,

        /// <summary>ISSN (encodes as EAN-13 with 977 prefix).</summary>
        Issn,

        /// <summary>
        /// MSI (Modified Plessey). Numeric-only barcode, commonly used for inventory.
        /// </summary>
        Msi,

        /// <summary>Telepen.</summary>
        Telepen
    }

    /// <summary>
    /// A 1D barcode generator component that renders barcodes as SVG graphics.
    /// Generates barcodes entirely client-side (no external dependencies).
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenBarcode Value="RADZEN-12345" Type="RadzenBarcodeType.Code128" Height="80px" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenBarcode : RadzenComponent
    {
        /// <summary>
        /// Gets whether the specified barcode <paramref name="type"/> produces a checksum value that can be displayed
        /// when <see cref="ShowChecksum"/> is enabled.
        /// </summary>
        /// <param name="type">The barcode type.</param>
        /// <returns><c>true</c> if the type has a checksum value; otherwise, <c>false</c>.</returns>
        public static bool HasChecksum(RadzenBarcodeType type) => type switch
        {
            RadzenBarcodeType.Code128 => true,
            RadzenBarcodeType.Ean13 => true,
            RadzenBarcodeType.Ean8 => true,
            RadzenBarcodeType.UpcA => true,
            RadzenBarcodeType.Postnet => true,
            RadzenBarcodeType.Rm4scc => true,
            RadzenBarcodeType.Isbn => true,
            RadzenBarcodeType.Issn => true,
            RadzenBarcodeType.Msi => true,
            RadzenBarcodeType.Telepen => true,
            _ => false
        };

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// </summary>
        [Parameter] public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the barcode type.
        /// </summary>
        [Parameter] public RadzenBarcodeType Type { get; set; } = RadzenBarcodeType.Code128;

        /// <summary>
        /// Gets or sets the rendered width of the SVG. Accepts CSS units (e.g. "300px", "100%").
        /// </summary>
        [Parameter] public string Width { get; set; } = "100%";

        /// <summary>
        /// Gets or sets the rendered height of the SVG. Accepts CSS units (e.g. "80px").
        /// If <see cref="ShowValue"/> is true, the text is drawn inside this height.
        /// </summary>
        [Parameter] public string Height { get; set; } = "80px";

        /// <summary>
        /// Gets or sets the barcode bars color.
        /// </summary>
        [Parameter] public string Foreground { get; set; } = "#000";

        /// <summary>
        /// Gets or sets the barcode background color.
        /// </summary>
        [Parameter] public string Background { get; set; } = "#FFF";

        /// <summary>
        /// Gets or sets the quiet zone in modules (left and right padding).
        /// </summary>
        [Parameter] public int QuietZoneModules { get; set; } = 10;

        /// <summary>
        /// Gets or sets the height of the bars in SVG units (viewBox units). Default is 50.
        /// </summary>
        [Parameter] public double BarHeight { get; set; } = 50;

        /// <summary>
        /// Gets or sets whether to show the value as text under the bars.
        /// </summary>
        [Parameter] public bool ShowValue { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show the checksum (if applicable for the selected <see cref="Type"/>) under the bars.
        /// </summary>
        [Parameter] public bool ShowChecksum { get; set; } = true;

        /// <summary>
        /// Gets or sets the font size for layout calculations of the value text (in SVG viewBox units).
        /// This is not automatically applied as an SVG attribute; use <see cref="ValueStyle"/> to style the text.
        /// </summary>
        [Parameter] public double FontSize { get; set; } = 12;

        /// <summary>
        /// Gets or sets the value inline CSS style.
        /// </summary>
        /// <value>The value style.</value>
        [Parameter]
        public virtual string? ValueStyle { get; set; }

        /// <summary>
        /// Gets or sets the gap between bars and text in SVG units (viewBox units).
        /// </summary>
        [Parameter] public double TextMarginTop { get; set; } = 6;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        protected override string GetComponentCssClass() => "rz-barcode";

        private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

        (IReadOnlyList<BarcodeRect> bars, double vbWidth, string? checksumText, string? error) GetBarcode()
        {
            if (string.IsNullOrEmpty(Value))
            {
                return (Array.Empty<BarcodeRect>(), 1, null, null);
            }

            try
            {
                return Type switch
                {
                    RadzenBarcodeType.Code128 => CreateFromWidths(RadzenBarcodeEncoder.EncodeCode128B(Value, out var c128), c128.ToString(CultureInfo.InvariantCulture)),
                    RadzenBarcodeType.Code39 => CreateFromWidths(RadzenBarcodeEncoder.EncodeCode39(Value), null),
                    RadzenBarcodeType.Codabar => CreateFromWidths(RadzenBarcodeEncoder.EncodeCodabar(Value), null),
                    RadzenBarcodeType.Itf => CreateFromWidths(RadzenBarcodeEncoder.EncodeItf(Value), null),
                    RadzenBarcodeType.Ean13 => CreateFromBits(RadzenBarcodeEncoder.EncodeEan13(Value, out var ean13Check), ean13Check),
                    RadzenBarcodeType.Ean8 => CreateFromBits(RadzenBarcodeEncoder.EncodeEan8(Value, out var ean8Check), ean8Check),
                    RadzenBarcodeType.UpcA => CreateFromBits(RadzenBarcodeEncoder.EncodeUpcA(Value, out var upcCheck), upcCheck),
                    RadzenBarcodeType.Isbn => CreateFromBits(RadzenBarcodeEncoder.EncodeIsbnAsEan13(Value, out var isbnCheck), isbnCheck),
                    RadzenBarcodeType.Issn => CreateFromBits(RadzenBarcodeEncoder.EncodeIssnAsEan13(Value, out var issnCheck), issnCheck),
                    RadzenBarcodeType.Pharmacode => CreateFromRects(RadzenBarcodeEncoder.EncodePharmacode(Value, BarHeight, QuietZoneModules), null),
                    RadzenBarcodeType.Postnet => CreateFromRects(RadzenBarcodeEncoder.EncodePostnet(Value, BarHeight, QuietZoneModules, out var postnetCheck), postnetCheck),
                    RadzenBarcodeType.Rm4scc => CreateFromRects(RadzenBarcodeEncoder.EncodeRm4scc(Value, BarHeight, QuietZoneModules, out var rmCheck), rmCheck),
                    RadzenBarcodeType.Msi => CreateFromBits(RadzenBarcodeEncoder.EncodeMsiPlessey(Value, out var msiCheck), msiCheck),
                    RadzenBarcodeType.Telepen => CreateFromWidths(RadzenBarcodeEncoder.EncodeTelepen(Value, out var telepenCheck), telepenCheck),
                    _ => (Array.Empty<BarcodeRect>(), 1, null, null)
                };
            }
            catch (Exception ex)
            {
                // Avoid breaking the whole page. Optionally, users can inspect via dev tools if needed.
                return (Array.Empty<BarcodeRect>(), 1, null, ex.Message);
            }
        }

        (IReadOnlyList<BarcodeRect> bars, double vbWidth, string? checksumText, string? error) CreateFromWidths(IReadOnlyList<int> widths, string? checksumText)
        {
            var rects = new List<BarcodeRect>();
            double x = Math.Max(0, QuietZoneModules);
            bool isBar = true;
            for (int i = 0; i < widths.Count; i++)
            {
                var w = widths[i];
                if (isBar && w > 0)
                {
                    rects.Add(new BarcodeRect(x, 0, w, BarHeight));
                }
                x += w;
                isBar = !isBar;
            }

            var vbWidth = x + Math.Max(0, QuietZoneModules);
            if (vbWidth <= 0) vbWidth = 1;
            return (rects, vbWidth, checksumText, null);
        }

        (IReadOnlyList<BarcodeRect> bars, double vbWidth, string? checksumText, string? error) CreateFromBits(string bits, string? checksumText)
        {
            if (string.IsNullOrEmpty(bits))
            {
                return (Array.Empty<BarcodeRect>(), 1, checksumText, null);
            }

            var rects = new List<BarcodeRect>();
            var quiet = Math.Max(0, QuietZoneModules);
            for (int i = 0; i < bits.Length;)
            {
                if (bits[i] != '1')
                {
                    i++;
                    continue;
                }

                int j = i + 1;
                while (j < bits.Length && bits[j] == '1') j++;
                rects.Add(new BarcodeRect(quiet + i, 0, j - i, BarHeight));
                i = j;
            }

            var vbWidth = quiet + bits.Length + quiet;
            if (vbWidth <= 0) vbWidth = 1;
            return (rects, vbWidth, checksumText, null);
        }

        (IReadOnlyList<BarcodeRect> bars, double vbWidth, string? checksumText, string? error) CreateFromRects((IReadOnlyList<BarcodeRect> bars, double vbWidth) geometry, string? checksumText)
        {
            var vbWidth = geometry.vbWidth;
            if (vbWidth <= 0) vbWidth = 1;
            return (geometry.bars, vbWidth, checksumText, null);
        }

        /// <summary>
        /// Returns the SVG markup of the rendered QR code as a string.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{String}"/> representing the asynchronous operation. The task result contains the SVG markup of the QR code.
        /// </returns>
        public async Task<string> ToSvg()
        {
            if (JSRuntime != null)
            {
                return await JSRuntime.InvokeAsync<string>("Radzen.outerHTML", Element);
            }
            return string.Empty;
        }
    }
}

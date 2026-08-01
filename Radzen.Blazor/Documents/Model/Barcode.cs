using Radzen.Documents.Fonts;
using Radzen.Documents.Codes;

namespace Radzen.Documents;


/// <summary>
/// A block that renders a 1D barcode as crisp vector bars - one filled black rectangle per bar.
/// The block hierarchy is closed: create barcodes through
/// <see cref="BlockCollection.AddBarcode(BarcodeType, string, Unit, Unit, bool)"/>.
/// </summary>
public sealed class Barcode : Block
{
    internal Barcode(BarcodeType type, string value, Unit width, Unit height)
    {
        Type = type;
        Value = value;
        Width = width;
        Height = height;
    }

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the barcode symbology.</summary>
    public BarcodeType Type { get; }

    /// <summary>Gets the encoded value.</summary>
    public string Value { get; }

    /// <summary>Gets the rendered width of the bars.</summary>
    public Unit Width { get; }

    /// <summary>Gets the rendered height of the bars.</summary>
    public Unit Height { get; }

    /// <summary>Gets or sets whether the human-readable value is drawn centered below the bars. Defaults to <see langword="false"/>.</summary>
    public bool ShowText { get; set; }

    /// <summary>Gets the font of the human-readable line.</summary>
    public Font Font { get; } = new();

    /// <summary>Gets or sets the horizontal alignment within the container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the alternate (accessibility) description of the barcode, carried on the
    /// barcode in accessible output for assistive technology to announce in place of the bars.
    /// Follows the HTML <c>alt</c> convention:
    /// <see langword="null"/> (the default) states nothing about the barcode - it stays a figure
    /// without a description, which accessible output rejects; the empty string declares the
    /// barcode purely decorative, so it is written as an artifact instead of a figure; a
    /// non-empty value is the description itself.
    /// </summary>
    public string? AlternateText { get; set; }
}

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A block that renders a 1D barcode as crisp vector bars - one filled black rectangle per bar.
/// </summary>
/// <param name="type">The barcode symbology.</param>
/// <param name="value">The value to encode.</param>
/// <param name="width">The rendered width of the bars.</param>
/// <param name="height">The rendered height of the bars, excluding the optional human-readable line.</param>
public sealed class Barcode(BarcodeType type, string value, Unit width, Unit height) : Block
{
    /// <summary>Gets the barcode symbology.</summary>
    public BarcodeType Type { get; } = type;

    /// <summary>Gets the encoded value.</summary>
    public string Value { get; } = value;

    /// <summary>Gets the rendered width of the bars.</summary>
    public Unit Width { get; } = width;

    /// <summary>Gets the rendered height of the bars.</summary>
    public Unit Height { get; } = height;

    /// <summary>Gets or sets whether the human-readable value is drawn centered below the bars. Defaults to <see langword="false"/>.</summary>
    public bool ShowText { get; set; }

    /// <summary>Gets the font of the human-readable line.</summary>
    public Font Font { get; } = new();

    /// <summary>Gets or sets the horizontal alignment within the container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    // Reserved below the bars for the human-readable line; slightly above typical line height so text never overlaps the next block.
    internal double TextBandHeight => ShowText ? Font.Size * 1.4 : 0;
}

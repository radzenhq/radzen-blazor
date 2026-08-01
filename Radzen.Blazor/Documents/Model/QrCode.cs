using Radzen.Documents.Codes;

namespace Radzen.Documents;


/// <summary>
/// A block that renders a QR code as crisp vector squares - one filled black square per dark module.
/// The block hierarchy is closed: create codes through
/// <see cref="BlockCollection.AddQrCode(string, Unit, QrErrorCorrection)"/>.
/// </summary>
public sealed class QrCode : Block
{
    private int quietZoneModules = 4;

    internal QrCode(string value, Unit size)
    {
        Value = value;
        Size = size;
    }

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the encoded text.</summary>
    public string Value { get; }

    /// <summary>Gets the rendered width and height, quiet zone included.</summary>
    public Unit Size { get; }

    /// <summary>Gets or sets the error-correction level. Defaults to <see cref="QrErrorCorrection.Medium"/>.</summary>
    public QrErrorCorrection ErrorCorrection { get; set; } = QrErrorCorrection.Medium;

    /// <summary>Gets or sets the quiet-zone width in modules on each side. Defaults to 4 (the QR specification minimum).</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public int QuietZoneModules
    {
        get => quietZoneModules;
        set => quietZoneModules = AuthoredNumber.NonNegative(value, "QrCode.QuietZoneModules");
    }

    /// <summary>Gets or sets the horizontal alignment within the container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the alternate (accessibility) description of the code, carried on the code in
    /// accessible output for assistive technology to announce in place of the modules.
    /// Follows the HTML <c>alt</c> convention:
    /// <see langword="null"/> (the default) states nothing about the code - it stays a figure
    /// without a description, which accessible output rejects; the empty string declares the
    /// code purely decorative, so it is written as an artifact instead of a figure; a
    /// non-empty value is the description itself.
    /// </summary>
    public string? AlternateText { get; set; }
}

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf;


/// <summary>
/// A block that renders a QR code as crisp vector squares - one filled black square per dark module.
/// </summary>
/// <param name="value">The text to encode.</param>
/// <param name="size">The rendered width and height of the code, quiet zone included.</param>
public sealed class QrCode(string value, Unit size) : Block
{
    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the encoded text.</summary>
    public string Value { get; } = value;

    /// <summary>Gets the rendered width and height, quiet zone included.</summary>
    public Unit Size { get; } = size;

    /// <summary>Gets or sets the error-correction level. Defaults to <see cref="QrErrorCorrection.Medium"/>.</summary>
    public QrErrorCorrection ErrorCorrection { get; set; } = QrErrorCorrection.Medium;

    /// <summary>Gets or sets the quiet-zone width in modules on each side. Defaults to 4 (the QR specification minimum).</summary>
    public int QuietZoneModules { get; set; } = 4;

    /// <summary>Gets or sets the horizontal alignment within the container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
}

using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF boolean object (ISO 32000-1 section 7.3.2), serialized as
/// <c>true</c> or <c>false</c>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BooleanObject"/> class.
/// </remarks>
/// <param name="value">The boolean value.</param>
public sealed class BooleanObject(bool value) : DocumentObject
{

    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; } = value;

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        PdfBytes.WriteAscii(stream, Value ? "true" : "false");
    }
}

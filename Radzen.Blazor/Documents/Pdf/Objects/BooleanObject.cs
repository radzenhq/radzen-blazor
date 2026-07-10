using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// A PDF boolean object (ISO 32000-1 section 7.3.2), serialized as
/// <c>true</c> or <c>false</c>.
/// </summary>
public sealed class BooleanObject : DocumentObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanObject"/> class.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public BooleanObject(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; }

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        PdfBytes.WriteAscii(stream, Value ? "true" : "false");
    }
}

using System.Globalization;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// An indirect reference to another object (ISO 32000-1 section 7.3.10),
/// serialized as <c>object generation R</c>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReferenceObject"/> class.
/// </remarks>
/// <param name="objectNumber">The referenced object number.</param>
/// <param name="generation">The referenced generation number.</param>
public sealed class ReferenceObject(int objectNumber, int generation) : DocumentObject
{

    /// <summary>
    /// Gets the referenced object number.
    /// </summary>
    public int ObjectNumber { get; } = objectNumber;

    /// <summary>
    /// Gets the referenced generation number.
    /// </summary>
    public int Generation { get; } = generation;

    /// <inheritdoc />
    public override void Write(Stream stream)
    {
        PdfBytes.WriteAscii(stream, string.Create(CultureInfo.InvariantCulture, $"{ObjectNumber} {Generation} R"));
    }
}

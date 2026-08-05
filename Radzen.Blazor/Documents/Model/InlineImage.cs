using System.IO;
using Radzen.Documents.Internal;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A raster image that flows inline within a paragraph line, sitting on the line's baseline and
/// advancing it by the image width. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class InlineImage : Inline
{
    private Unit? width;
    private Unit? height;

    internal InlineImage(byte[] data) => Data = data;

    /// <summary>
    /// Initializes an inline image, buffering the bytes from the specified stream. The caller may
    /// dispose the stream immediately afterwards.
    /// </summary>
    /// <param name="stream">The source image stream.</param>
    /// <param name="limits">The resource limits bounding the buffered bytes, or <see langword="null"/> for <see cref="ResourceLimits.Default"/>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidDataException">The stream holds more than <see cref="ResourceLimits.MaxFileBytes"/> bytes.</exception>
    public InlineImage(Stream stream, ResourceLimits? limits = null)
        : this(ReadFully(stream, limits))
    {
    }

    private static byte[] ReadFully(Stream stream, ResourceLimits? limits)
    {
        System.ArgumentNullException.ThrowIfNull(stream);
        return StreamBytes.ReadFully(stream, (limits ?? ResourceLimits.Default).MaxFileBytes);
    }

    internal byte[] Data { get; }

    /// <summary>
    /// Gets or sets the drawn width, or <see langword="null"/> (the default) for the natural width.
    /// When only <see cref="Height"/> is set the width follows from it and the image's aspect ratio.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Width
    {
        get => width;
        set => width = AuthoredNumber.AbsolutePositive(value, "InlineImage.Width");
    }

    /// <summary>
    /// Gets or sets the drawn height, or <see langword="null"/> (the default) for the natural height.
    /// When only <see cref="Width"/> is set the height follows from it and the image's aspect ratio.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Height
    {
        get => height;
        set => height = AuthoredNumber.AbsolutePositive(value, "InlineImage.Height");
    }

    /// <summary>
    /// Gets or sets the alternate (accessibility) description of the image, carried on the
    /// image in accessible output for assistive technology to announce in place of the picture.
    /// Follows the HTML <c>alt</c> convention:
    /// <see langword="null"/> (the default) states nothing about the image - it stays a figure
    /// without a description, which accessible output rejects unless <see cref="ReplacementText"/> is set;
    /// the empty string declares the image purely decorative, so it is written as an artifact
    /// instead of a figure; a non-empty value is the description itself.
    /// </summary>
    public string? AlternateText { get; set; }

    /// <summary>
    /// Gets or sets the replacement text of the image: the exact text the picture stands in
    /// for, carried on the image in accessible output so that extraction and reading substitute
    /// it for the picture. When <see langword="null"/> or empty (the default) no replacement text is written.
    /// </summary>
    public string? ReplacementText { get; set; }
}

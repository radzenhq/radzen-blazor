using System;
using System.IO;
using Radzen.Documents.Internal;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A block that renders a raster image. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class Image : Block
{
    private double opacity = 1;
    private Unit? width;
    private Unit? height;
    private (Unit MaxWidth, Unit MaxHeight)? fitBox;

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    internal Image(byte[] data) => Data = data;

    internal static Image FromStream(Stream stream, ResourceLimits? limits = null)
        => new(StreamBytes.ReadFully(stream, (limits ?? ResourceLimits.Default).MaxFileBytes));

    internal byte[] Data { get; }

    /// <summary>Gets or sets the rendered width. When <see langword="null"/> the natural width is used.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Width
    {
        get => width;
        set => width = AuthoredNumber.AbsolutePositive(value, "Image.Width");
    }

    /// <summary>Gets or sets the rendered height. When <see langword="null"/> the natural height is used.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is relative or is not greater than zero.</exception>
    public Unit? Height
    {
        get => height;
        set => height = AuthoredNumber.AbsolutePositive(value, "Image.Height");
    }

    /// <summary>Gets or sets the horizontal alignment of the image within its container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

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

    /// <summary>
    /// Gets or sets the opacity the image is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Image");
    }

    /// <summary>
    /// Gets or sets whether the viewer should smooth the image when it is drawn at a size
    /// other than its natural pixel dimensions. When <see langword="true"/> the request for
    /// smoothing is carried with the image. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Interpolate { get; set; }

    /// <summary>
    /// Gets or sets the box the image is scaled to fit, or <see langword="null"/> (the default)
    /// for none. Setting <see langword="null"/> clears it. The image keeps its aspect ratio: the
    /// smaller of the two scale factors is applied so it fits both bounds. The base aspect is
    /// taken from any explicit <see cref="Width"/>/<see cref="Height"/>, otherwise from the
    /// image's natural size.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A bound is relative or is not greater than zero.</exception>
    public (Unit MaxWidth, Unit MaxHeight)? FitBox
    {
        get => fitBox;
        set => fitBox = value is { } box
            ? (AuthoredNumber.AbsolutePositive(box.MaxWidth, "Image.FitBox.MaxWidth"),
                AuthoredNumber.AbsolutePositive(box.MaxHeight, "Image.FitBox.MaxHeight"))
            : null;
    }
}

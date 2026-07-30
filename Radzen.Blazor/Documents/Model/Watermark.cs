using System;
using System.IO;
using Radzen.Documents.Fonts;

namespace Radzen.Documents;


/// <summary>
/// A stamp drawn over every page of a <see cref="Section"/>: text and/or an image,
/// rotated around the page center and painted with a constant opacity.
/// </summary>
public sealed class Watermark
{
    private double opacity = 0.15;
    private double rotation = 45;

    /// <summary>Gets or sets the watermark text, or <see langword="null"/> for none.</summary>
    public string? Text { get; set; }

    /// <summary>Gets the font of the watermark text. Defaults to 72 pt.</summary>
    public Font Font { get; } = new() { Size = 72 };

    /// <summary>
    /// Gets or sets the opacity the watermark is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 0.15.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Watermark");
    }

    /// <summary>
    /// Gets or sets the counterclockwise rotation in degrees around the page center.
    /// Defaults to 45.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not finite.</exception>
    public double Rotation
    {
        get => rotation;
        set => rotation = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Watermark rotation must be finite.");
    }

    /// <summary>
    /// Gets or sets the image drawn centered on the page under any <see cref="Text"/>, or
    /// <see langword="null"/> (the default) for none. Setting <see langword="null"/> clears it.
    /// </summary>
    public Image? Image { get; set; }

    /// <summary>
    /// Sets the watermark image, drawn centered on the page under any <see cref="Text"/>.
    /// The stream is buffered fully so it may be closed immediately after.
    /// </summary>
    /// <param name="image">A stream containing the image data.</param>
    /// <returns>The buffered <see cref="Image"/>; its sizing members control the drawn size.</returns>
    public Image SetImage(Stream image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var buffered = Radzen.Documents.Image.FromStream(image);
        Image = buffered;
        return buffered;
    }
}

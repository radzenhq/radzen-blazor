using System;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf.Emit;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A stamp drawn over every page of a <see cref="Section"/>: text and/or an image,
/// rotated around the page center and painted with a constant opacity.
/// </summary>
public sealed class Watermark
{
    /// <summary>Gets or sets the watermark text, or <see langword="null"/> for none.</summary>
    public string? Text { get; set; }

    /// <summary>Gets the font of the watermark text. Defaults to 72 pt.</summary>
    public Font Font { get; } = new() { Size = 72 };

    /// <summary>
    /// Gets or sets the opacity the watermark is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 0.15.
    /// </summary>
    public double Opacity { get; set; } = 0.15;

    /// <summary>
    /// Gets or sets the counterclockwise rotation in degrees around the page center.
    /// Defaults to 45.
    /// </summary>
    public double Rotation { get; set; } = 45;

    internal Image? Image { get; private set; }

    private ImageXObject? decoded;
    private (bool Interpolate, bool Stencil, int[]? ColorKeyMask) decodedOptions;

    // One Watermark is stamped onto every page, so its image decodes once and every page
    // shares the XObject; decoding per page would inflate the payload once per page and
    // emit a duplicate image stream for each. Keyed on the options ApplyOptions reads so a
    // caller that flips one between saves is not served a stale XObject.
    internal ImageXObject DecodeImage(Image image)
    {
        var options = (image.Interpolate, image.Stencil, image.ColorKeyMask);
        if (decoded is null || decodedOptions.Interpolate != options.Interpolate
            || decodedOptions.Stencil != options.Stencil
            || !ColorKeyMaskEqual(decodedOptions.ColorKeyMask, options.ColorKeyMask))
        {
            decoded = ImageDecoder.ApplyOptions(ImageDecoder.Decode(image.Data), image);
            decodedOptions = (options.Interpolate, options.Stencil, options.ColorKeyMask?.ToArray());
        }

        return decoded;
    }

    private static bool ColorKeyMaskEqual(int[]? first, int[]? second)
        => first is null ? second is null : second is not null && first.AsSpan().SequenceEqual(second);

    /// <summary>
    /// Sets the watermark image, drawn centered on the page under any <see cref="Text"/>.
    /// The stream is buffered fully so it may be closed immediately after.
    /// </summary>
    /// <param name="image">A stream containing the image data.</param>
    /// <returns>The buffered <see cref="Pdf.Image"/>; its sizing members control the drawn size.</returns>
    public Image SetImage(Stream image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Image = Image.FromStream(image);
        return Image;
    }
}

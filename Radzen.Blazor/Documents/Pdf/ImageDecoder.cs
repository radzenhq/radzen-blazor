using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf;

internal static class ImageDecoder
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jp2Signature = [0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A];

    public static ImageXObject Decode(byte[] imageBytes) => Decode(imageBytes, ReaderLimits.Default);

    public static ImageXObject Decode(byte[] imageBytes, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(limits);

        if (IsPng(imageBytes))
        {
            return DecodePng(imageBytes, limits);
        }

        if (imageBytes.Length >= 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
        {
            return DecodeJpeg(imageBytes, limits);
        }

        if (IsJpeg2000(imageBytes))
        {
            return DecodeJpeg2000(imageBytes, limits);
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    // Rendered size honoring explicit Width/Height, keeping aspect when one is omitted.
    // A fully unsized image renders at 96dpi and is clamped to the available width.
    public static (double Width, double Height) Measure(Image image, ImageXObject xobject, double availableWidth)
    {
        var dict = xobject.Image.Dictionary;
        var pixelWidth = ((NumberObject)dict["Width"]).DoubleValue;
        var pixelHeight = ((NumberObject)dict["Height"]).DoubleValue;

        double baseWidth;
        double baseHeight;
        if (image.Width is { } w && image.Height is { } h)
        {
            baseWidth = w.Point;
            baseHeight = h.Point;
        }
        else if (image.Width is { } wo)
        {
            baseWidth = wo.Point;
            baseHeight = pixelHeight * wo.Point / pixelWidth;
        }
        else if (image.Height is { } ho)
        {
            baseWidth = pixelWidth * ho.Point / pixelHeight;
            baseHeight = ho.Point;
        }
        else
        {
            baseWidth = pixelWidth * 72.0 / 96.0;
            baseHeight = pixelHeight * 72.0 / 96.0;
        }

        if (image.FitBox is { } box)
        {
            var scale = System.Math.Min(box.Width.Point / baseWidth, box.Height.Point / baseHeight);
            return (baseWidth * scale, baseHeight * scale);
        }

        // Only a fully unsized image is clamped to the available width; an explicit dimension wins.
        if (image.Width is null && image.Height is null
            && availableWidth > 0 && !double.IsInfinity(availableWidth) && baseWidth > availableWidth)
        {
            baseHeight *= availableWidth / baseWidth;
            baseWidth = availableWidth;
        }

        return (baseWidth, baseHeight);
    }

    // Stamps the opt-in options a block Image carries onto its decoded XObject. A stencil
    // mask needs a dictionary without /ColorSpace, so it yields a fresh image-mask XObject;
    // the remaining flags are additive. An image that opts into nothing is returned untouched,
    // keeping the default output byte-identical.
    public static ImageXObject ApplyOptions(ImageXObject xobject, Image image)
    {
        if (image.Stencil && image.ColorKeyMask is not null)
        {
            throw new InvalidOperationException("Colour-key masking cannot be combined with a stencil mask.");
        }

        if (image.Stencil)
        {
            // Yields a brand-new image-mask XObject; nothing of the shared decode is touched.
            xobject = BuildStencilMask(xobject);
        }
        else if (image.ColorKeyMask is not null || image.Interpolate)
        {
            // Copy the shared decode before stamping additive dictionary options so the
            // cached plain XObject is never mutated in place (distinct-option uses of the
            // same bytes must not leak /Mask or /Interpolate onto each other).
            xobject = Copy(xobject);
        }

        if (image.ColorKeyMask is { } ranges)
        {
            ApplyColorKeyMask(xobject, ranges);
        }

        if (image.Interpolate)
        {
            xobject.Image.Dictionary["Interpolate"] = new BooleanObject(true);
        }

        return xobject;
    }

    // Shallow copy of an image XObject: a fresh stream over the same sample bytes carrying
    // a fresh dictionary with the same entries, so additive options mutate the copy alone.
    private static ImageXObject Copy(ImageXObject xobject)
    {
        var stream = new StreamObject(xobject.Image.Data);
        foreach (var key in xobject.Image.Dictionary.Keys)
        {
            stream.Dictionary[key] = xobject.Image.Dictionary[key];
        }

        return new ImageXObject(stream, xobject.SoftMask);
    }

    // Colour-key masking (ISO 32000-1 8.9.6.4): /Mask holds one inclusive [min max] pair per
    // colour component; a pixel whose components all fall within their ranges is not painted.
    private static void ApplyColorKeyMask(ImageXObject xobject, int[] ranges)
    {
        if (ranges.Length == 0 || (ranges.Length % 2) != 0)
        {
            throw new ArgumentException("A colour-key mask must be a non-empty sequence of [min max] pairs.", nameof(ranges));
        }

        if (xobject.SoftMask is not null)
        {
            throw new InvalidOperationException("Colour-key masking cannot be combined with an image that has an alpha channel.");
        }

        var dict = xobject.Image.Dictionary;
        if (ColorComponents(dict) is { } components && ranges.Length != 2 * components)
        {
            throw new ArgumentException(
                $"A colour-key mask needs one [min max] pair per colour component ({components}).", nameof(ranges));
        }

        var array = new ArrayObject();
        foreach (var value in ranges)
        {
            array.Add(new NumberObject(value));
        }

        dict["Mask"] = array;
    }

    // Colour-component count of the image's colour space, or null when it cannot be told from
    // the dictionary (a JPXDecode image carries its own colour space and has no /ColorSpace).
    private static int? ColorComponents(DictionaryObject dict)
    {
        if (!dict.TryGetValue("ColorSpace", out var colorSpace) || colorSpace is null)
        {
            return null;
        }

        return colorSpace switch
        {
            NameObject { Value: "DeviceGray" } => 1,
            NameObject { Value: "DeviceRGB" } => 3,
            NameObject { Value: "DeviceCMYK" } => 4,
            ArrayObject { Count: > 0 } array when array[0] is NameObject { Value: "Indexed" } => 1,
            _ => null,
        };
    }

    // Re-expresses a 1-bit grayscale image as a stencil mask (ISO 32000-1 8.9.6.2): the same
    // packed 1-bit sample stream, but with /ImageMask true and no /ColorSpace so its samples
    // gate painting in the current fill colour (sample 0 paints, per the default /Decode [0 1]).
    private static ImageXObject BuildStencilMask(ImageXObject xobject)
    {
        var source = xobject.Image.Dictionary;
        if (xobject.SoftMask is not null
            || source["ColorSpace"] is not NameObject { Value: "DeviceGray" }
            || ((NumberObject)source["BitsPerComponent"]).IntValue != 1)
        {
            throw new InvalidOperationException(
                "A stencil mask requires a 1-bit grayscale image with no alpha channel.");
        }

        var stream = new StreamObject(xobject.Image.Data);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = source["Width"];
        dict["Height"] = source["Height"];
        dict["ImageMask"] = new BooleanObject(true);
        dict["BitsPerComponent"] = new NumberObject(1);
        dict["Filter"] = source["Filter"];
        return new ImageXObject(stream, null);
    }

    private static bool IsPng(byte[] data)
    {
        if (data.Length < PngSignature.Length)
        {
            return false;
        }

        for (var i = 0; i < PngSignature.Length; i++)
        {
            if (data[i] != PngSignature[i])
            {
                return false;
            }
        }

        return true;
    }

    private static ImageXObject DecodePng(byte[] data, ReaderLimits limits)
    {
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();

        long pos = PngSignature.Length;
        while (pos + 8 <= data.Length)
        {
            // The chunk length is an unsigned 32-bit count; reject one that runs past the
            // buffer so a hostile length (e.g. a 0x80000000 PLTE) cannot slice out of range.
            uint length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            long body = pos + 8;
            if (length > data.Length - body)
            {
                throw new InvalidDataException("PNG chunk length exceeds the available data.");
            }

            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var start = (int)body;
            var count = (int)length;

            switch (type)
            {
                case "IHDR":
                    if (count < 13)
                    {
                        throw new InvalidDataException("PNG IHDR chunk is truncated.");
                    }

                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(start));
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(start + 4));
                    bitDepth = data[start + 8];
                    colorType = data[start + 9];
                    if (data[start + 12] != 0)
                    {
                        throw new NotSupportedException("Adam7 interlaced PNG images are not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data[start..(start + count)];
                    break;
                case "tRNS":
                    transparency = data[start..(start + count)];
                    break;
                case "IDAT":
                    idat.Write(data, start, count);
                    break;
                case "IEND":
                    pos = data.Length;
                    continue;
            }

            pos = body + count + 4;
        }

        ValidateImageDimensions(width, height, limits, "PNG");

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };

        ValidatePngBitDepth(colorType, bitDepth);

        var compressed = idat.ToArray();
        var raw = FlateFilter.Decode(compressed, limits.MaxDecodedStreamBytes);

        // Secondary compression-bomb guard mirroring DocumentReader: a tiny IDAT that inflates
        // past the ratio ceiling is rejected once the decoded output clears the floor.
        if (raw.LongLength > limits.ExpansionRatioFloorBytes
            && compressed.Length > 0
            && raw.LongLength / compressed.Length > limits.MaxDecodeExpansionRatio)
        {
            throw new InvalidDataException("PNG image data expansion ratio exceeds the maximum.");
        }

        var samples = PngPredictor.Decode(raw, channels, bitDepth, width);

        // A truncated IDAT decodes to fewer scanlines than IHDR promises; the downstream
        // per-pixel unpackers index samples by the header dimensions and would read out of
        // range. Reject a short sample buffer rather than crashing or emitting garbage.
        var bytesPerRow = (((long)width * channels * bitDepth) + 7) / 8;
        if (samples.Length < (long)height * bytesPerRow)
        {
            throw new InvalidDataException("PNG image data is truncated.");
        }

        return colorType switch
        {
            0 => BuildColorKeyedPng(width, height, bitDepth, new NameObject("DeviceGray"), samples, transparency, 1),
            2 => BuildColorKeyedPng(width, height, bitDepth, new NameObject("DeviceRGB"), samples, transparency, 3),
            3 => BuildPalettedPng(width, height, bitDepth, samples, palette, transparency),
            4 => BuildAlphaPng(width, height, new NameObject("DeviceGray"), samples, 1, bitDepth),
            6 => BuildAlphaPng(width, height, new NameObject("DeviceRGB"), samples, 3, bitDepth),
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };
    }

    // PNG restricts which bit depths a colour type may use (ISO/IEC 15948 Table 11.1);
    // an out-of-table pair (e.g. RGBA at 4-bit) yields a zero-stride sample layout that
    // would silently decode to garbage, so reject it up front.
    private static void ValidatePngBitDepth(int colorType, int bitDepth)
    {
        var valid = colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            2 => bitDepth is 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            4 => bitDepth is 8 or 16,
            6 => bitDepth is 8 or 16,
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidDataException($"PNG bit depth {bitDepth} is not allowed for colour type {colorType}.");
        }
    }

    // Grayscale/truecolor samples pass straight through; a tRNS chunk on these colour types
    // is a colour key rather than an alpha channel, mapped to /Mask per ISO 32000-1 8.9.6.4.
    private static ImageXObject BuildColorKeyedPng(
        int width, int height, int bitDepth, NameObject colorSpace, byte[] samples, byte[]? transparency, int components)
    {
        var image = BuildImage(width, height, bitDepth, colorSpace, samples);

        if (transparency is not null)
        {
            if (transparency.Length < 2 * components)
            {
                throw new InvalidDataException("PNG tRNS chunk is truncated for the colour type.");
            }

            // tRNS stores one 16-bit key per component regardless of bit depth; the key value
            // is already in the sample's integer range, so it maps directly to a [key key] pair.
            var mask = new ArrayObject();
            for (var c = 0; c < components; c++)
            {
                var value = BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(c * 2));
                mask.Add(new NumberObject(value));
                mask.Add(new NumberObject(value));
            }

            image.Dictionary["Mask"] = mask;
        }

        return new ImageXObject(image, null);
    }

    // Header dimensions drive every downstream pixel buffer; reject non-positive or
    // oversized geometry before allocating so a tiny header cannot request gigabytes
    // or wrap width*height negative.
    private static void ValidateImageDimensions(long width, long height, ReaderLimits limits, string format)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"{format} image has invalid dimensions.");
        }

        if (width * height > limits.MaxImagePixels)
        {
            throw new InvalidDataException($"{format} image dimensions exceed the maximum decodable size.");
        }
    }

    private static ImageXObject BuildPalettedPng(
        int width,
        int height,
        int bitDepth,
        byte[] indices,
        byte[]? palette,
        byte[]? transparency)
    {
        if (palette is null)
        {
            throw new InvalidDataException("Paletted PNG is missing its PLTE chunk.");
        }

        if (palette.Length == 0 || palette.Length % 3 != 0 || palette.Length / 3 > 256)
        {
            throw new InvalidDataException("PNG PLTE chunk must hold 1 to 256 RGB triples.");
        }

        var hival = (palette.Length / 3) - 1;
        var colorSpace = new ArrayObject
        {
            new NameObject("Indexed"),
            new NameObject("DeviceRGB"),
            new NumberObject(hival),
            new StringObject(BytesToString(palette)),
        };

        var image = BuildImage(width, height, bitDepth, colorSpace, indices);

        if (transparency is null)
        {
            return new ImageXObject(image, null);
        }

        // The color path passes packed indices straight to PDF, but the 8-bit soft mask needs one index per pixel.
        var pixels = UnpackIndices(indices, width, height, bitDepth);
        var alpha = new byte[width * height];
        for (var i = 0; i < alpha.Length; i++)
        {
            var index = pixels[i];
            alpha[i] = index < transparency.Length ? transparency[index] : (byte)0xFF;
        }

        var mask = BuildImage(width, height, 8, new NameObject("DeviceGray"), alpha);
        return new ImageXObject(image, mask);
    }

    // Expand a paletted scanline buffer, where sub-8-bit indices are packed MSB-first and each row is byte padded, to one index per pixel.
    private static byte[] UnpackIndices(byte[] indices, int width, int height, int bitDepth)
    {
        if (bitDepth == 8)
        {
            return indices;
        }

        var rowLength = ((width * bitDepth) + 7) / 8;
        var mask = (1 << bitDepth) - 1;
        var pixels = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * rowLength;
            for (var x = 0; x < width; x++)
            {
                var bit = x * bitDepth;
                var shift = 8 - bitDepth - (bit % 8);
                pixels[(y * width) + x] = (byte)((indices[rowStart + (bit / 8)] >> shift) & mask);
            }
        }

        return pixels;
    }

    private static ImageXObject BuildAlphaPng(int width, int height, NameObject colorSpace, byte[] samples, int colorChannels, int bitDepth)
    {
        var pixelCount = width * height;
        // PNG restricts gray+alpha/RGBA to 8 or 16 bit; 16-bit samples are big-endian, downsampled to their high byte.
        var bytesPerSample = bitDepth / 8;
        var stride = (colorChannels + 1) * bytesPerSample;
        var color = new byte[pixelCount * colorChannels];
        var alpha = new byte[pixelCount];

        for (var i = 0; i < pixelCount; i++)
        {
            for (var c = 0; c < colorChannels; c++)
            {
                color[(i * colorChannels) + c] = samples[(i * stride) + (c * bytesPerSample)];
            }

            alpha[i] = samples[(i * stride) + (colorChannels * bytesPerSample)];
        }

        var image = BuildImage(width, height, 8, colorSpace, color);
        var mask = BuildImage(width, height, 8, new NameObject("DeviceGray"), alpha);
        return new ImageXObject(image, mask);
    }

    private static StreamObject BuildImage(int width, int height, int bitsPerComponent, DocumentObject colorSpace, byte[] samples)
    {
        var stream = new StreamObject(FlateFilter.Encode(samples));
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(width);
        dict["Height"] = new NumberObject(height);
        dict["ColorSpace"] = colorSpace;
        dict["BitsPerComponent"] = new NumberObject(bitsPerComponent);
        dict["Filter"] = new NameObject("FlateDecode");
        return stream;
    }

    private static ImageXObject DecodeJpeg(byte[] data, ReaderLimits limits)
    {
        var (width, height, precision, components, adobe) = ReadJpegFrame(data);

        ValidateImageDimensions(width, height, limits, "JPEG");

        var colorSpace = components switch
        {
            1 => "DeviceGray",
            3 => "DeviceRGB",
            4 => "DeviceCMYK",
            _ => throw new NotSupportedException($"Unsupported JPEG component count {components}."),
        };

        var stream = new StreamObject(data);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(width);
        dict["Height"] = new NumberObject(height);
        dict["ColorSpace"] = new NameObject(colorSpace);
        dict["BitsPerComponent"] = new NumberObject(precision);
        dict["Filter"] = new NameObject("DCTDecode");

        // The inverted /Decode is correct only for Adobe CMYK JPEGs, which store inverted
        // samples and are flagged by an APP14 'Adobe' marker; a non-Adobe CMYK JPEG holds
        // normal samples and would render inverted if we forced the same array.
        if (components == 4 && adobe)
        {
            dict["Decode"] = new ArrayObject
            {
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
                new NumberObject(1), new NumberObject(0),
            };
        }

        return new ImageXObject(stream, null);
    }

    private static (int Width, int Height, int Precision, int Components, bool Adobe) ReadJpegFrame(byte[] data)
    {
        var adobe = false;
        var pos = 2;
        while (pos + 1 < data.Length)
        {
            if (data[pos] != 0xFF)
            {
                pos++;
                continue;
            }

            var marker = data[pos + 1];
            pos += 2;

            if (marker is 0x01 or (>= 0xD0 and <= 0xD9))
            {
                continue;
            }

            if (pos + 1 >= data.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos));
            if (segmentLength < 2 || pos + segmentLength > data.Length)
            {
                throw new InvalidDataException("JPEG segment length is invalid.");
            }

            if (marker == 0xEE && IsAdobeApp14(data, pos, segmentLength))
            {
                adobe = true;
            }

            if (IsStartOfFrame(marker))
            {
                // SOF3 (lossless), SOF5-7 (differential) and SOF9-15 (arithmetic-coded) cannot be
                // shown by a /DCTDecode viewer, which supports only baseline/extended-sequential/
                // progressive Huffman; embedding them verbatim would render blank. Fail loud.
                if (marker is not (0xC0 or 0xC1 or 0xC2))
                {
                    throw new NotSupportedException(
                        $"JPEG start-of-frame marker 0xFF{marker:X2} is not supported by DCTDecode.");
                }

                if (pos + 8 > data.Length)
                {
                    throw new InvalidDataException("JPEG start-of-frame segment is truncated.");
                }

                var precision = data[pos + 2];
                var height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 3));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 5));
                var components = data[pos + 7];
                return (width, height, precision, components, adobe);
            }

            pos += segmentLength;
        }

        throw new InvalidDataException("No JPEG start-of-frame marker was found.");
    }

    private static bool IsAdobeApp14(byte[] data, int pos, int segmentLength)
        => segmentLength >= 8
            && data[pos + 2] == (byte)'A'
            && data[pos + 3] == (byte)'d'
            && data[pos + 4] == (byte)'o'
            && data[pos + 5] == (byte)'b'
            && data[pos + 6] == (byte)'e';

    private static bool IsStartOfFrame(byte marker) => marker switch
    {
        >= 0xC0 and <= 0xC3 => true,
        >= 0xC5 and <= 0xC7 => true,
        >= 0xC9 and <= 0xCB => true,
        >= 0xCD and <= 0xCF => true,
        _ => false,
    };

    // A JP2 file opens with the signature box; a bare codestream opens with SOC+SIZ.
    private static bool IsJpeg2000(byte[] data)
        => StartsWith(data, Jp2Signature)
            || (data.Length >= 4 && data[0] == 0xFF && data[1] == 0x4F && data[2] == 0xFF && data[3] == 0x51);

    // JPEG2000 embeds verbatim through the /JPXDecode filter, exactly like the /DCTDecode
    // JPEG path: only the header is parsed for geometry and no /ColorSpace is written, so
    // the JPX stream's own colour space applies (PDF 32000-1 7.4.9). BitsPerComponent is
    // informational for JPXDecode; a conforming producer writes 8.
    private static ImageXObject DecodeJpeg2000(byte[] data, ReaderLimits limits)
    {
        var (width, height, components) = StartsWith(data, Jp2Signature)
            ? ReadJp2Header(data)
            : ReadCodestreamSiz(data, 2);

        ValidateJpeg2000Dimensions(width, height, components, limits);

        var stream = new StreamObject(data);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(width);
        dict["Height"] = new NumberObject(height);
        dict["BitsPerComponent"] = new NumberObject(8);
        dict["Filter"] = new NameObject("JPXDecode");
        return new ImageXObject(stream, null);
    }

    // Walk the top-level boxes: an ihdr (inside the jp2h superbox) gives dimensions
    // directly; otherwise fall back to the SIZ marker inside the jp2c codestream.
    private static (int Width, int Height, int Components) ReadJp2Header(byte[] data)
    {
        long codestream = -1;
        var pos = (long)Jp2Signature.Length;
        while (pos + 8 <= data.Length)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var contentStart = pos + 8;
            long contentEnd = length == 0 ? data.Length : pos + length;
            if (length == 1 || contentEnd < contentStart || contentEnd > data.Length)
            {
                throw new InvalidDataException("JPEG2000 box length is invalid.");
            }

            if (type == "jp2h")
            {
                var ihdr = FindIhdr(data, contentStart, contentEnd);
                if (ihdr is { } dims)
                {
                    return dims;
                }
            }
            else if (type == "jp2c" && codestream < 0)
            {
                codestream = contentStart;
            }

            pos = contentEnd;
        }

        if (codestream >= 0)
        {
            return ReadCodestreamSiz(data, codestream + 2);
        }

        throw new InvalidDataException("JPEG2000 file has no ihdr or codestream.");
    }

    private static (int Width, int Height, int Components)? FindIhdr(byte[] data, long start, long end)
    {
        var pos = start;
        while (pos + 8 <= end)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)pos));
            var type = Encoding.ASCII.GetString(data, (int)pos + 4, 4);
            var contentStart = pos + 8;
            long contentEnd = length == 0 ? end : pos + length;
            if (length == 1 || contentEnd < contentStart || contentEnd > end)
            {
                throw new InvalidDataException("JPEG2000 box length is invalid.");
            }

            if (type == "ihdr")
            {
                if (contentEnd - contentStart < 10)
                {
                    throw new InvalidDataException("JPEG2000 ihdr box is truncated.");
                }

                var height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart));
                var width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)contentStart + 4));
                var components = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan((int)contentStart + 8));
                return (width, height, components);
            }

            pos = contentEnd;
        }

        return null;
    }

    // sizPos points just past the SOC marker; the SIZ marker segment carries Xsiz/Ysiz,
    // the image origin XOsiz/YOsiz, and the component count Csiz.
    private static (int Width, int Height, int Components) ReadCodestreamSiz(byte[] data, long sizPos)
    {
        if (sizPos + 40 > data.Length || data[sizPos] != 0xFF || data[sizPos + 1] != 0x51)
        {
            throw new InvalidDataException("JPEG2000 codestream is missing its SIZ marker.");
        }

        var body = (int)sizPos + 2;
        var xsiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 4));
        var ysiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 8));
        var xosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 12));
        var yosiz = (long)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 16));
        var components = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(body + 36));

        var width = xsiz - xosiz;
        var height = ysiz - yosiz;
        if (width is <= 0 or > int.MaxValue || height is <= 0 or > int.MaxValue)
        {
            throw new InvalidDataException("JPEG2000 codestream has invalid dimensions.");
        }

        return ((int)width, (int)height, components);
    }

    private static void ValidateJpeg2000Dimensions(int width, int height, int components, ReaderLimits limits)
    {
        if (components <= 0)
        {
            throw new InvalidDataException("JPEG2000 image has invalid dimensions.");
        }

        ValidateImageDimensions(width, height, limits, "JPEG2000");
    }

    private static bool StartsWith(byte[] data, byte[] prefix)
    {
        if (data.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (data[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string BytesToString(byte[] bytes)
    {
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i] = (char)bytes[i];
        }

        return new string(chars);
    }
}

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

    public static ImageXObject Decode(byte[] imageBytes)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        if (IsPng(imageBytes))
        {
            return DecodePng(imageBytes);
        }

        if (imageBytes.Length >= 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
        {
            return DecodeJpeg(imageBytes);
        }

        throw new NotSupportedException("Unrecognized image format; only PNG and JPEG are supported.");
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

    private static ImageXObject DecodePng(byte[] data)
    {
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();

        var pos = PngSignature.Length;
        while (pos + 8 <= data.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos));
            var type = Encoding.ASCII.GetString(data, pos + 4, 4);
            var body = pos + 8;

            switch (type)
            {
                case "IHDR":
                    width = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body));
                    height = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(body + 4));
                    bitDepth = data[body + 8];
                    colorType = data[body + 9];
                    if (data[body + 12] != 0)
                    {
                        throw new NotSupportedException("Adam7 interlaced PNG images are not supported.");
                    }

                    break;
                case "PLTE":
                    palette = data[body..(body + length)];
                    break;
                case "tRNS":
                    transparency = data[body..(body + length)];
                    break;
                case "IDAT":
                    idat.Write(data, body, length);
                    break;
                case "IEND":
                    pos = data.Length;
                    continue;
            }

            pos = body + length + 4;
        }

        var channels = colorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };

        var raw = FlateFilter.Decode(idat.ToArray());
        var samples = PngPredictor.Decode(raw, channels, bitDepth, width);

        return colorType switch
        {
            0 => new ImageXObject(BuildImage(width, height, bitDepth, new NameObject("DeviceGray"), samples), null),
            2 => new ImageXObject(BuildImage(width, height, bitDepth, new NameObject("DeviceRGB"), samples), null),
            3 => BuildPalettedPng(width, height, bitDepth, samples, palette, transparency),
            4 => BuildAlphaPng(width, height, new NameObject("DeviceGray"), samples, 1, bitDepth),
            6 => BuildAlphaPng(width, height, new NameObject("DeviceRGB"), samples, 3, bitDepth),
            _ => throw new NotSupportedException($"Unsupported PNG colour type {colorType}."),
        };
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

    private static ImageXObject DecodeJpeg(byte[] data)
    {
        var (width, height, precision, components) = ReadJpegFrame(data);

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

        if (components == 4)
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

    private static (int Width, int Height, int Precision, int Components) ReadJpegFrame(byte[] data)
    {
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

            if (IsStartOfFrame(marker))
            {
                var precision = data[pos + 2];
                var height = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 3));
                var width = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 5));
                var components = data[pos + 7];
                return (width, height, precision, components);
            }

            pos += segmentLength;
        }

        throw new InvalidDataException("No JPEG start-of-frame marker was found.");
    }

    private static bool IsStartOfFrame(byte marker) => marker switch
    {
        >= 0xC0 and <= 0xC3 => true,
        >= 0xC5 and <= 0xC7 => true,
        >= 0xC9 and <= 0xCB => true,
        >= 0xCD and <= 0xCF => true,
        _ => false,
    };

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

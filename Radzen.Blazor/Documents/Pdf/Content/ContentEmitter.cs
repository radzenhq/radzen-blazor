using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf.Content;

// Pure byte-writing helpers: given a ContentWriter and a draw command, they emit the
// operators for that command. No state, so they stay static and shared across pages.
internal static class ContentEmitter
{
    public static void WriteClipRect(ContentWriter writer, in Rect clip)
    {
        writer.WriteNumber(clip.X);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Y);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Width);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Height);
        writer.WriteRaw(" re W n\n");
    }

    // Sets the clip to a rounded rectangle when radius > 0, otherwise to the plain rectangle.
    public static void WriteClip(ContentWriter writer, in Rect clip, double radius)
    {
        if (radius > 0)
        {
            WriteRoundedRect(writer, clip.X, clip.Y, clip.Width, clip.Height, radius);
            writer.WriteRaw("W n\n");
        }
        else
        {
            WriteClipRect(writer, clip);
        }
    }

    // Circle-approximation constant for a quarter arc drawn as one cubic Bezier.
    private const double BezierArcKappa = 0.5522847498307936;

    // Writes a rounded-rectangle path (m/l/c ops, closed with h) starting at the bottom-left
    // corner's end point and running counterclockwise. The caller appends the paint operator.
    public static void WriteRoundedRect(ContentWriter writer, double x, double y, double width, double height, double radius)
    {
        var offset = radius * BezierArcKappa;
        var right = x + width;
        var top = y + height;
        WritePoint(writer, x + radius, y, " m\n");
        WritePoint(writer, right - radius, y, " l\n");
        WriteCurve(writer, right - radius + offset, y, right, y + radius - offset, right, y + radius);
        WritePoint(writer, right, top - radius, " l\n");
        WriteCurve(writer, right, top - radius + offset, right - radius + offset, top, right - radius, top);
        WritePoint(writer, x + radius, top, " l\n");
        WriteCurve(writer, x + radius - offset, top, x, top - radius + offset, x, top - radius);
        WritePoint(writer, x, y + radius, " l\n");
        WriteCurve(writer, x, y + radius - offset, x + radius - offset, y, x + radius, y);
        writer.WriteRaw("h\n");
    }

    private static void WritePoint(ContentWriter writer, double x, double y, string op)
    {
        writer.WriteNumber(x);
        writer.WriteRaw(" ");
        writer.WriteNumber(y);
        writer.WriteRaw(op);
    }

    private static void WriteCurve(ContentWriter writer, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        WritePoint(writer, x1, y1, " ");
        WritePoint(writer, x2, y2, " ");
        WritePoint(writer, x3, y3, " c\n");
    }

    // Emits "a b c d e f cm" concatenating the transform with the CTM. Must sit inside a
    // caller-managed q .. Q pair so the surrounding graphics state stays untouched.
    public static void WriteTransform(ContentWriter writer, in Matrix matrix)
    {
        writer.WriteNumber(matrix.A);
        writer.WriteRaw(" ");
        writer.WriteNumber(matrix.B);
        writer.WriteRaw(" ");
        writer.WriteNumber(matrix.C);
        writer.WriteRaw(" ");
        writer.WriteNumber(matrix.D);
        writer.WriteRaw(" ");
        writer.WriteNumber(matrix.E);
        writer.WriteRaw(" ");
        writer.WriteNumber(matrix.F);
        writer.WriteRaw(" cm\n");
    }

    public static void WriteImageDraw(ContentWriter writer, in ImageDraw image)
    {
        writer.WriteRaw("q\n");
        if (image.ExtGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        // The transform precedes the clip so the clip rectangle (given in the same
        // pre-transform coordinates as the image box) rotates with the image.
        if (image.Transform is { } transform)
        {
            WriteTransform(writer, transform);
        }

        if (image.Clip is { } clip)
        {
            WriteClip(writer, clip, image.ClipRadius);
        }

        if (image.StencilColor is { } stencilColor)
        {
            writer.WriteColor(stencilColor, "rg");
        }

        writer.WriteNumber(image.Width);
        writer.WriteRaw(" 0 0 ");
        writer.WriteNumber(image.Height);
        writer.WriteRaw(" ");
        writer.WriteNumber(image.X);
        writer.WriteRaw(" ");
        writer.WriteNumber(image.Y);
        writer.WriteRaw(" cm\n");
        writer.WriteName(image.Image.Key);
        writer.WriteRaw(" Do\nQ\n");
    }

    public static void WriteTextDraw(ContentWriter writer, in TextDraw text)
    {
        var wrapped = text.ExtGState is not null || text.Clip is not null || text.Transform is not null;
        if (wrapped)
        {
            writer.WriteRaw("q\n");
        }

        if (text.ExtGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        if (text.Transform is { } transform)
        {
            WriteTransform(writer, transform);
        }

        if (text.Clip is { } clip)
        {
            WriteClip(writer, clip, text.ClipRadius);
        }

        writer.WriteRaw("BT\n");
        if (text.FillPaint is { } fillPaint)
        {
            WriteDeviceFill(writer, fillPaint);
        }
        else
        {
            writer.WriteColor(text.Color, "rg");
        }

        writer.WriteName(text.Font.Key);
        writer.WriteRaw(" ");
        writer.WriteNumber(text.Size);
        writer.WriteRaw(" Tf\n");
        if (text.CharSpacing != 0)
        {
            writer.WriteNumber(text.CharSpacing);
            writer.WriteRaw(" Tc\n");
        }

        var wordSpacing = text.WordSpacing != 0;
        if (wordSpacing)
        {
            writer.WriteNumber(text.WordSpacing);
            writer.WriteRaw(" Tw\n");
        }

        // 0 (the struct default for draws that never set it) means "unchanged"; only a
        // genuinely non-100 scale emits Tz, so default text stays byte identical.
        var horizontalScale = text.HorizontalScale != 0 && text.HorizontalScale != 100;
        if (horizontalScale)
        {
            writer.WriteNumber(text.HorizontalScale);
            writer.WriteRaw(" Tz\n");
        }

        if (text.Rise != 0)
        {
            writer.WriteNumber(text.Rise);
            writer.WriteRaw(" Ts\n");
        }

        // Synthetic bold draws in fill+stroke (mode 2); an explicit invisible/other mode
        // wins. Both reset to 0 Tr after the show since Tr persists across BT/ET.
        var renderMode = text.RenderMode != 0 ? text.RenderMode : text.StrokeWidth > 0 ? 2 : 0;
        if (text.StrokeWidth > 0 && renderMode == 2)
        {
            writer.WriteColor(text.Color, "RG");
            writer.WriteNumber(text.StrokeWidth);
            writer.WriteRaw(" w\n");
        }

        if (renderMode != 0)
        {
            writer.WriteNumber(renderMode);
            writer.WriteRaw(" Tr\n");
        }

        if (text.Shear != 0)
        {
            writer.WriteRaw("1 0 ");
            writer.WriteNumber(text.Shear);
            writer.WriteRaw(" 1 ");
            writer.WriteNumber(text.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(text.Baseline);
            writer.WriteRaw(" Tm\n");
        }
        else
        {
            writer.WriteNumber(text.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(text.Baseline);
            writer.WriteRaw(" Td\n");
        }

        if (text.Kerns is { } kerns)
        {
            WriteKernedShow(writer, RemapBytes(text), kerns, text.Font.Base14 is not null ? 1 : 2);
        }
        else
        {
            writer.WriteString(RemapBytes(text));
            writer.WriteRaw(" Tj\n");
        }

        if (renderMode != 0)
        {
            writer.WriteRaw("0 Tr\n");
        }

        // Tc/Ts/Tw/Tz persist across BT/ET, so non-default values are reset after the show.
        if (text.CharSpacing != 0)
        {
            writer.WriteRaw("0 Tc\n");
        }

        if (wordSpacing)
        {
            writer.WriteRaw("0 Tw\n");
        }

        if (horizontalScale)
        {
            writer.WriteRaw("100 Tz\n");
        }

        if (text.Rise != 0)
        {
            writer.WriteRaw("0 Ts\n");
        }

        writer.WriteRaw("ET\n");
        if (wrapped)
        {
            writer.WriteRaw("Q\n");
        }
    }

    // Emits a device fill colour (Gray g, CMYK k or a named colorspace cs+scn) in place of rg.
    private static void WriteDeviceFill(ContentWriter writer, DeviceColor color)
    {
        if (color.Kind == DeviceColorKind.Named && color.ColorSpace is { } name)
        {
            writer.WriteName(name);
            writer.WriteRaw(" cs\n");
        }

        foreach (var operand in color.Operands)
        {
            writer.WriteNumber(operand);
            writer.WriteRaw(" ");
        }

        writer.WriteRaw(color.Kind switch
        {
            DeviceColorKind.Named => "scn",
            DeviceColorKind.Gray => "g",
            _ => "k",
        });
        writer.WriteRaw("\n");
    }

    // Shows a glyph string as a TJ array with per-pair kern adjustments interleaved: each
    // kern is a TJ number (positive tightens) placed between adjacent glyph codes. Glyph
    // codes are 2 bytes for embedded Type0 subsets and 1 byte for WinAnsi base-14 faces.
    private static void WriteKernedShow(ContentWriter writer, byte[] bytes, double[] kerns, int glyphWidth)
    {
        writer.WriteRaw("[");
        var glyphs = bytes.Length / glyphWidth;
        for (var g = 0; g < glyphs; g++)
        {
            if (g > 0 && kerns[g - 1] != 0)
            {
                writer.WriteNumber(kerns[g - 1]);
                writer.WriteRaw(" ");
            }

            writer.WriteString(bytes[(g * glyphWidth)..((g + 1) * glyphWidth)]);
        }

        writer.WriteRaw("] TJ\n");
    }

    // Layout emits original gids; the compact map renumbers them into the embedded
    // subset's 0..N-1 space so the 2-byte Identity-H code equals the new gid.
    public static byte[] RemapBytes(in TextDraw text)
    {
        if (text.Font.CompactGidMap is not { } gidMap)
        {
            return text.Bytes;
        }

        var bytes = text.Bytes;
        var remapped = new byte[bytes.Length];
        for (var i = 0; i + 1 < bytes.Length; i += 2)
        {
            var gid = gidMap[(ushort)((bytes[i] << 8) | bytes[i + 1])];
            remapped[i] = (byte)(gid >> 8);
            remapped[i + 1] = (byte)gid;
        }

        return remapped;
    }
}

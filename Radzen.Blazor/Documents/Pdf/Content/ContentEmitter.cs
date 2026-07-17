using System;
using System.Collections.Immutable;
using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf.Content;

// The operands of one BT..ET text show. Capability differences between callers are values
// here (an unset one emits nothing), not separate emit paths.
internal readonly struct TextShowOp
{
    public required string FontKey { get; init; }
    public required double Size { get; init; }
    public required double X { get; init; }
    public required double Baseline { get; init; }
    public Color Color { get; init; }
    public DeviceColor? FillPaint { get; init; }
    public double CharSpacing { get; init; }
    public double WordSpacing { get; init; }
    public double HorizontalScale { get; init; }
    public double Rise { get; init; }
    public int RenderMode { get; init; }
    public double StrokeWidth { get; init; }
    public double Shear { get; init; }

    // The show payload, already prepared by the caller: Adjustments wins (verbatim TJ array),
    // else Kerns (per-pair kerned TJ over Bytes), else a plain Tj of Bytes.
    public ReadOnlyMemory<byte> Bytes { get; init; }
    public double[]? Kerns { get; init; }
    public int GlyphWidth { get; init; }
    public ImmutableArray<TextAdjustment>? Adjustments { get; init; }

    // Emits the trailing 0 Tr / 0 Tc / 0 Tw / 100 Tz / 0 Ts resets. The loaded-content path
    // leaves the state set, so re-emitted bytes match what was read.
    public bool ResetTextState { get; init; }

    // Set when this show splices into a text object the caller has already opened. ISO
    // 32000-1 9.4.1 forbids nesting one, and a nested BT would reset the text and line
    // matrices the enclosing object's later operators still position against. The live text
    // matrix already sits at the run's origin, so no BT/ET and no Td are emitted; X and
    // Baseline must be zero because the caller carries the origin in the ambient instead.
    public bool InsideTextObject { get; init; }
}

// Pure byte-writing helpers: given a ContentWriter and a draw command, they emit the
// operators for that command. No state, so they stay static and shared across pages.
internal static class ContentEmitter
{
    public static void WriteClipRect(ContentWriter writer, in PdfRect clip)
    {
        writer.WriteNumber(clip.Left);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Bottom);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Width);
        writer.WriteRaw(" ");
        writer.WriteNumber(clip.Height);
        writer.WriteRaw(" re W n\n");
    }

    // Sets the clip to a rounded rectangle when radius > 0, otherwise to the plain rectangle.
    public static void WriteClip(ContentWriter writer, in PdfRect clip, double radius)
    {
        if (radius > 0)
        {
            WriteRoundedRect(writer, clip.Left, clip.Bottom, clip.Width, clip.Height, radius);
            writer.WriteRaw("W n\n");
        }
        else
        {
            WriteClipRect(writer, clip);
        }
    }

    // Writes a rounded-rectangle path (m/l/c ops, closed with h) starting at the bottom-left
    // corner's end point and running counterclockwise. The caller appends the paint operator.
    public static void WriteRoundedRect(ContentWriter writer, double x, double y, double width, double height, double radius)
    {
        var offset = radius * BezierGeometry.Kappa;
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

        WriteTextShow(writer, new TextShowOp
        {
            FontKey = text.Font.Key,
            Size = text.Size,
            X = text.X,
            Baseline = text.Baseline,
            Color = text.Color,
            FillPaint = text.FillPaint,
            CharSpacing = text.CharSpacing,
            WordSpacing = text.WordSpacing,
            HorizontalScale = text.HorizontalScale,
            Rise = text.Rise,
            RenderMode = text.RenderMode,
            StrokeWidth = text.StrokeWidth,
            Shear = text.Shear,
            Bytes = RemapBytes(text),
            Kerns = text.Kerns,
            GlyphWidth = text.Font.Base14 is not null ? 1 : 2,
            ResetTextState = true,
        });

        if (wrapped)
        {
            writer.WriteRaw("Q\n");
        }
    }

    // The single BT..ET text-show skeleton. Callers own their own wrapper (q/gs/cm/clip) and
    // their own payload preparation; everything between BT and ET is emitted here so the
    // operator order and the state-reset discipline exist once.
    public static void WriteTextShow(ContentWriter writer, in TextShowOp op)
    {
        if (op.InsideTextObject && (op.X != 0 || op.Baseline != 0 || op.Shear != 0))
        {
            throw new NotSupportedException("A text show spliced into an open text object must carry its origin in the ambient transform.");
        }

        if (!op.InsideTextObject)
        {
            writer.WriteRaw("BT\n");
        }

        if (op.FillPaint is { } fillPaint)
        {
            WriteDeviceColor(writer, fillPaint, stroke: false);
        }
        else
        {
            writer.WriteColor(op.Color, "rg");
        }

        writer.WriteName(op.FontKey);
        writer.WriteRaw(" ");
        writer.WriteNumber(op.Size);
        writer.WriteRaw(" Tf\n");
        if (op.CharSpacing != 0)
        {
            writer.WriteNumber(op.CharSpacing);
            writer.WriteRaw(" Tc\n");
        }

        if (op.WordSpacing != 0)
        {
            writer.WriteNumber(op.WordSpacing);
            writer.WriteRaw(" Tw\n");
        }

        // 0 (the default for draws that never set it) means "unchanged"; only a genuinely
        // non-100 scale emits Tz, so default text stays byte identical.
        var horizontalScale = op.HorizontalScale != 0 && op.HorizontalScale != 100;
        if (horizontalScale)
        {
            writer.WriteNumber(op.HorizontalScale);
            writer.WriteRaw(" Tz\n");
        }

        if (op.Rise != 0)
        {
            writer.WriteNumber(op.Rise);
            writer.WriteRaw(" Ts\n");
        }

        // Synthetic bold draws in fill+stroke (mode 2); an explicit invisible/other mode
        // wins. Both reset to 0 Tr after the show since Tr persists across BT/ET.
        var renderMode = op.RenderMode != 0 ? op.RenderMode : op.StrokeWidth > 0 ? 2 : 0;
        if (op.StrokeWidth > 0 && renderMode == 2)
        {
            writer.WriteColor(op.Color, "RG");
            writer.WriteNumber(op.StrokeWidth);
            writer.WriteRaw(" w\n");
        }

        if (renderMode != 0)
        {
            writer.WriteNumber(renderMode);
            writer.WriteRaw(" Tr\n");
        }

        // Positioning inside an open text object would discard where that object left the
        // text and line matrices; the ambient transform carries the origin instead.
        if (!op.InsideTextObject)
        {
            WritePosition(writer, op);
        }

        WriteShow(writer, op);
        if (op.ResetTextState)
        {
            if (renderMode != 0)
            {
                writer.WriteRaw("0 Tr\n");
            }

            // Tc/Ts/Tw/Tz persist across BT/ET, so non-default values are reset after the show.
            if (op.CharSpacing != 0)
            {
                writer.WriteRaw("0 Tc\n");
            }

            if (op.WordSpacing != 0)
            {
                writer.WriteRaw("0 Tw\n");
            }

            if (horizontalScale)
            {
                writer.WriteRaw("100 Tz\n");
            }

            if (op.Rise != 0)
            {
                writer.WriteRaw("0 Ts\n");
            }
        }

        if (!op.InsideTextObject)
        {
            writer.WriteRaw("ET\n");
        }
    }

    // Shear needs the full Tm form; a plain origin rides the shorter Td.
    private static void WritePosition(ContentWriter writer, in TextShowOp op)
    {
        if (op.Shear != 0)
        {
            writer.WriteRaw("1 0 ");
            writer.WriteNumber(op.Shear);
            writer.WriteRaw(" 1 ");
            writer.WriteNumber(op.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(op.Baseline);
            writer.WriteRaw(" Tm\n");
        }
        else
        {
            writer.WriteNumber(op.X);
            writer.WriteRaw(" ");
            writer.WriteNumber(op.Baseline);
            writer.WriteRaw(" Td\n");
        }
    }

    private static void WriteShow(ContentWriter writer, in TextShowOp op)
    {
        if (op.Adjustments is { } segments)
        {
            writer.WriteRaw("[");
            foreach (var segment in segments)
            {
                if (segment.Text is not null)
                {
                    writer.WriteString(segment.Text);
                }
                else
                {
                    writer.WriteNumber(segment.Adjustment);
                    writer.WriteRaw(" ");
                }
            }

            writer.WriteRaw("] TJ\n");
        }
        else if (op.Kerns is { } kerns)
        {
            WriteKernedShow(writer, op.Bytes.Span, kerns, op.GlyphWidth);
        }
        else
        {
            writer.WriteString(op.Bytes.Span);
            writer.WriteRaw(" Tj\n");
        }
    }

    // Emits a device colour (Gray g/G, CMYK k/K or a named colorspace cs+scn / CS+SCN) in
    // place of rg/RG.
    public static void WriteDeviceColor(ContentWriter writer, DeviceColor color, bool stroke)
    {
        if (color.Kind == DeviceColorKind.Named && color.ColorSpace is { } name)
        {
            writer.WriteName(name);
            writer.WriteRaw(stroke ? " CS\n" : " cs\n");
        }

        foreach (var operand in color.Operands)
        {
            writer.WriteNumber(operand);
            writer.WriteRaw(" ");
        }

        if (color.PatternName is { } pattern)
        {
            writer.WriteName(pattern);
            writer.WriteRaw(" ");
        }

        writer.WriteRaw(color.Kind switch
        {
            DeviceColorKind.Named => stroke ? "SCN" : "scn",
            DeviceColorKind.Gray => stroke ? "G" : "g",
            _ => stroke ? "K" : "k",
        });
        writer.WriteRaw("\n");
    }

    // Shows a glyph string as a TJ array with per-pair kern adjustments interleaved: each
    // kern is a TJ number (positive tightens) placed between adjacent glyph codes. Glyph
    // codes are 2 bytes for embedded Type0 subsets and 1 byte for WinAnsi base-14 faces.
    private static void WriteKernedShow(ContentWriter writer, ReadOnlySpan<byte> bytes, double[] kerns, int glyphWidth)
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

            writer.WriteString(bytes.Slice(g * glyphWidth, glyphWidth));
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

using System;
using System.Collections.Immutable;
using Radzen.Documents.Core;
namespace Radzen.Documents.Pdf.Content;

internal readonly struct TextShowOp
{
    public required string FontKey { get; init; }
    public required double Size { get; init; }
    public required double X { get; init; }
    public required double Baseline { get; init; }
    public Color Color { get; init; }
    public string? ExtGState { get; init; }
    public DeviceColor? FillPaint { get; init; }
    public double CharSpacing { get; init; }
    public double WordSpacing { get; init; }
    public double HorizontalScale { get; init; }
    public double Rise { get; init; }
    public int RenderMode { get; init; }
    public double StrokeWidth { get; init; }
    public double Shear { get; init; }

    public ReadOnlyMemory<byte> Bytes { get; init; }
    public double[]? Kerns { get; init; }
    public int GlyphWidth { get; init; }
    public ImmutableArray<TextAdjustment>? Adjustments { get; init; }

    public bool ResetTextState { get; init; }

    // ISO 32000-1 9.4.1 forbids nesting text objects.
    public bool InsideTextObject { get; init; }
}

internal static class ContentEmitter
{
    public static void WriteWatermark(
        ContentWriter writer,
        string? extGState,
        in Matrix transform,
        Action<ContentWriter> writeImage,
        Action<ContentWriter> writeText)
    {
        writer.WriteRaw("q\n");
        if (extGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        WriteTransform(writer, transform);
        writeImage(writer);
        writeText(writer);
        writer.WriteRaw("Q\n");
    }

    public static void WriteClipRect(ContentWriter writer, in PdfRect clip)
    {
        WriteRectangle(writer, clip.Left, clip.Bottom, clip.Width, clip.Height);
        writer.WriteRaw(" W n\n");
    }

    public static void WriteRectangle(ContentWriter writer, double x, double y, double width, double height)
    {
        writer.WriteNumber(x);
        writer.WriteRaw(" ");
        writer.WriteNumber(y);
        writer.WriteRaw(" ");
        writer.WriteNumber(width);
        writer.WriteRaw(" ");
        writer.WriteNumber(height);
        writer.WriteRaw(" re");
    }

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

    public static void WriteStrokeWidth(ContentWriter writer, double width)
    {
        writer.WriteNumber(width);
        writer.WriteRaw(" w\n");
    }

    public static void WriteDashPattern(ContentWriter writer, ReadOnlySpan<double> pattern, double phase)
    {
        writer.WriteRaw("[");
        for (var i = 0; i < pattern.Length; i++)
        {
            if (i > 0)
            {
                writer.WriteRaw(" ");
            }

            writer.WriteNumber(pattern[i]);
        }

        writer.WriteRaw("] ");
        writer.WriteNumber(phase);
        writer.WriteRaw(" d\n");
    }

    public static void WriteStrokeState(ContentWriter writer, Color color, double lineWidth, BorderStyle style)
    {
        writer.WriteColor(color, "RG");
        WriteStrokeWidth(writer, lineWidth);

        if (style is not (BorderStyle.Dashed or BorderStyle.Dotted))
        {
            return;
        }

        var on = (style == BorderStyle.Dashed ? 3.0 : 1.0) * lineWidth;
        Span<double> pattern = [on, on];
        WriteDashPattern(writer, pattern, 0);
    }

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

    public static void WriteImagePlacement(
        ContentWriter writer, string key, double x, double y, double width, double height,
        string? extGState = null, Matrix? transform = null, PdfRect? clip = null,
        double clipRadius = 0)
    {
        writer.WriteRaw("q\n");
        if (extGState is { } state)
        {
            writer.WriteName(state);
            writer.WriteRaw(" gs\n");
        }

        if (transform is { } t)
        {
            WriteTransform(writer, t);
        }

        if (clip is { } c)
        {
            WriteClip(writer, c, clipRadius);
        }

        writer.WriteNumber(width);
        writer.WriteRaw(" 0 0 ");
        writer.WriteNumber(height);
        writer.WriteRaw(" ");
        writer.WriteNumber(x);
        writer.WriteRaw(" ");
        writer.WriteNumber(y);
        writer.WriteRaw(" cm\n");
        writer.WriteName(key);
        writer.WriteRaw(" Do\nQ\n");
    }

    public static void WriteTextShow(ContentWriter writer, in TextShowOp op)
    {
        if (op.InsideTextObject && (op.X != 0 || op.Baseline != 0 || op.Shear != 0))
        {
            throw new NotSupportedException("A text show spliced into an open text object must carry its origin in the ambient transform.");
        }

        if (op.ExtGState is { } extGState)
        {
            writer.WriteRaw("q\n");
            writer.WriteName(extGState);
            writer.WriteRaw(" gs\n");
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

        var horizontalScale = op.HorizontalScale != 0 && op.HorizontalScale != 1;
        if (horizontalScale)
        {
            writer.WriteNumber(op.HorizontalScale * 100);
            writer.WriteRaw(" Tz\n");
        }

        if (op.Rise != 0)
        {
            writer.WriteNumber(op.Rise);
            writer.WriteRaw(" Ts\n");
        }

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

        if (op.ExtGState is not null)
        {
            writer.WriteRaw("Q\n");
        }
    }

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
            _ => stroke ? "K" : "k",
        });
        writer.WriteRaw("\n");
    }

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
}

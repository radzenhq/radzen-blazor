using Radzen.Documents.Pdf.Content;
using static Radzen.Documents.Pdf.Content.ContentEmitter;

namespace Radzen.Documents.Pdf.Render;

internal static class DrawEmitter
{
    public static void WriteImageDraw(ContentWriter writer, in ImageDraw image) =>
        WriteImagePlacement(writer, image.Image.Key, image.X, image.Y, image.Width, image.Height,
            image.ExtGState, image.Transform, image.Clip, image.ClipRadius);

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

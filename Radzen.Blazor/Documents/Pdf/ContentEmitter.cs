namespace Radzen.Documents.Pdf;

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

    public static void WriteImageDraw(ContentWriter writer, in ImageDraw image)
    {
        writer.WriteRaw("q\n");
        if (image.Clip is { } clip)
        {
            WriteClipRect(writer, clip);
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
        if (text.Clip is { } clip)
        {
            writer.WriteRaw("q\n");
            WriteClipRect(writer, clip);
        }

        writer.WriteRaw("BT\n");
        writer.WriteColor(text.Color, "rg");
        writer.WriteName(text.Font.Key);
        writer.WriteRaw(" ");
        writer.WriteNumber(text.Size);
        writer.WriteRaw(" Tf\n");
        if (text.CharSpacing != 0)
        {
            writer.WriteNumber(text.CharSpacing);
            writer.WriteRaw(" Tc\n");
        }

        if (text.Rise != 0)
        {
            writer.WriteNumber(text.Rise);
            writer.WriteRaw(" Ts\n");
        }

        if (text.StrokeWidth > 0)
        {
            writer.WriteColor(text.Color, "RG");
            writer.WriteNumber(text.StrokeWidth);
            writer.WriteRaw(" w\n2 Tr\n");
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

        writer.WriteString(RemapBytes(text));
        writer.WriteRaw(" Tj\n");
        if (text.StrokeWidth > 0)
        {
            writer.WriteRaw("0 Tr\n");
        }

        // Tc/Ts persist across BT/ET, so non-default values are reset after the show.
        if (text.CharSpacing != 0)
        {
            writer.WriteRaw("0 Tc\n");
        }

        if (text.Rise != 0)
        {
            writer.WriteRaw("0 Ts\n");
        }

        writer.WriteRaw("ET\n");
        if (text.Clip is not null)
        {
            writer.WriteRaw("Q\n");
        }
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

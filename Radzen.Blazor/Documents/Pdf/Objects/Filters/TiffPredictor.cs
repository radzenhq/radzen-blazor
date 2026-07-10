using System;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class TiffPredictor
{
    public static byte[] Decode(byte[] data, int colors, int bitsPerComponent, int columns)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (bitsPerComponent != 8)
        {
            throw new NotSupportedException("Only 8 bits per component is supported.");
        }

        int rowLength = colors * columns;
        if (rowLength == 0)
        {
            return Array.Empty<byte>();
        }

        var output = (byte[])data.Clone();
        int rows = data.Length / rowLength;

        for (int row = 0; row < rows; row++)
        {
            int start = row * rowLength;
            for (int i = colors; i < rowLength; i++)
            {
                output[start + i] = (byte)(output[start + i] + output[start + i - colors]);
            }
        }

        return output;
    }
}

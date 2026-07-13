using System;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class TiffPredictor
{
    private const int MaxColors = 32;

    public static byte[] Decode(byte[] data, int colors, int bitsPerComponent, int columns)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (bitsPerComponent != 8)
        {
            throw new NotSupportedException("Only 8 bits per component is supported.");
        }

        // colors/columns come straight from an attacker-controlled /DecodeParms, so validate
        // them and compute the row length as a long: colors*columns overflows int32 for a
        // hostile /Columns and would otherwise wrap to zero (whole stream dropped) or negative.
        if (colors <= 0 || colors > MaxColors || columns <= 0)
        {
            throw new DocumentParseException("TIFF predictor colors/columns are out of range.");
        }

        if (data.Length == 0)
        {
            return [];
        }

        long rowLength = (long)colors * columns;
        if (data.Length % rowLength != 0)
        {
            throw new DocumentParseException("TIFF predictor data is not a whole number of rows.");
        }

        var output = (byte[])data.Clone();
        int stride = (int)rowLength;
        int rows = data.Length / stride;

        for (int row = 0; row < rows; row++)
        {
            int start = row * stride;
            for (int i = colors; i < stride; i++)
            {
                output[start + i] = (byte)(output[start + i] + output[start + i - colors]);
            }
        }

        return output;
    }
}

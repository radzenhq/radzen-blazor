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

        PredictorParameters.ValidateColorsAndColumns(colors, columns, "TIFF");

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

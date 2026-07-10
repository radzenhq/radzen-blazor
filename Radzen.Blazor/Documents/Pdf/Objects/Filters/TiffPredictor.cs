using System;

namespace Radzen.Documents.Pdf.Objects.Filters
{
    /// <summary>
    /// Reverses TIFF predictor 2 (horizontal differencing) as used by the PDF
    /// <c>FlateDecode</c> and <c>LZWDecode</c> filters. Supports 8 bits per component.
    /// </summary>
    public static class TiffPredictor
    {
        /// <summary>
        /// Reverses TIFF predictor 2. Each component adds the reconstructed value of the
        /// same component in the previous sample.
        /// </summary>
        /// <param name="data">The horizontally differenced rows.</param>
        /// <param name="colors">Number of colour components per sample.</param>
        /// <param name="bitsPerComponent">Bits per colour component (only 8 is supported).</param>
        /// <param name="columns">Number of samples per row.</param>
        /// <returns>The reconstructed raw bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="bitsPerComponent"/> is not 8.</exception>
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
}

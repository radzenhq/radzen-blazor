using System;

namespace Radzen.Documents.Pdf.Objects.Filters
{
    /// <summary>
    /// Applies and reverses PNG predictors (PDF <c>Predictor</c> values 10-15) as used by
    /// the <c>FlateDecode</c> and <c>LZWDecode</c> filters.
    /// </summary>
    public static class PngPredictor
    {
        /// <summary>
        /// Reverses PNG prediction. Each row is prefixed with a PNG filter-type byte (0-4).
        /// </summary>
        /// <param name="data">The predicted rows, each prefixed with its filter-type byte.</param>
        /// <param name="colors">Number of colour components per sample.</param>
        /// <param name="bitsPerComponent">Bits per colour component.</param>
        /// <param name="columns">Number of samples per row.</param>
        /// <returns>The reconstructed raw bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        public static byte[] Decode(byte[] data, int colors, int bitsPerComponent, int columns)
        {
            ArgumentNullException.ThrowIfNull(data);

            int bpp = BytesPerPixel(colors, bitsPerComponent);
            int rowLength = RowLength(colors, bitsPerComponent, columns);
            if (rowLength == 0)
            {
                return Array.Empty<byte>();
            }

            int stride = rowLength + 1;
            int rows = data.Length / stride;
            var output = new byte[rows * rowLength];

            var prior = new byte[rowLength];
            for (int row = 0; row < rows; row++)
            {
                int filter = data[row * stride];
                int src = row * stride + 1;
                int dst = row * rowLength;

                for (int i = 0; i < rowLength; i++)
                {
                    int raw = data[src + i];
                    int a = i >= bpp ? output[dst + i - bpp] : 0;
                    int b = prior[i];
                    int c = i >= bpp ? prior[i - bpp] : 0;

                    int value = filter switch
                    {
                        0 => raw,
                        1 => raw + a,
                        2 => raw + b,
                        3 => raw + ((a + b) >> 1),
                        4 => raw + Paeth(a, b, c),
                        _ => throw new InvalidOperationException($"Unsupported PNG filter type {filter}."),
                    };

                    output[dst + i] = (byte)value;
                }

                Array.Copy(output, dst, prior, 0, rowLength);
            }

            return output;
        }

        /// <summary>
        /// Applies PNG prediction using the given PDF predictor value.
        /// </summary>
        /// <param name="data">The raw rows.</param>
        /// <param name="predictor">The PDF predictor value (10=None, 11=Sub, 12=Up, 13=Average, 14=Paeth).</param>
        /// <param name="colors">Number of colour components per sample.</param>
        /// <param name="bitsPerComponent">Bits per colour component.</param>
        /// <param name="columns">Number of samples per row.</param>
        /// <returns>The predicted rows, each prefixed with its filter-type byte.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        public static byte[] Encode(byte[] data, int predictor, int colors, int bitsPerComponent, int columns)
        {
            ArgumentNullException.ThrowIfNull(data);

            int bpp = BytesPerPixel(colors, bitsPerComponent);
            int rowLength = RowLength(colors, bitsPerComponent, columns);
            if (rowLength == 0)
            {
                return Array.Empty<byte>();
            }

            int filter = predictor >= 10 ? predictor - 10 : predictor;
            if (filter < 0 || filter > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(predictor));
            }

            int rows = data.Length / rowLength;
            int stride = rowLength + 1;
            var output = new byte[rows * stride];

            var prior = new byte[rowLength];
            for (int row = 0; row < rows; row++)
            {
                int src = row * rowLength;
                int dst = row * stride;
                output[dst] = (byte)filter;

                for (int i = 0; i < rowLength; i++)
                {
                    int raw = data[src + i];
                    int a = i >= bpp ? data[src + i - bpp] : 0;
                    int b = prior[i];
                    int c = i >= bpp ? prior[i - bpp] : 0;

                    int value = filter switch
                    {
                        0 => raw,
                        1 => raw - a,
                        2 => raw - b,
                        3 => raw - ((a + b) >> 1),
                        4 => raw - Paeth(a, b, c),
                        _ => raw,
                    };

                    output[dst + 1 + i] = (byte)value;
                }

                Array.Copy(data, src, prior, 0, rowLength);
            }

            return output;
        }

        static int BytesPerPixel(int colors, int bitsPerComponent) =>
            Math.Max(1, colors * bitsPerComponent / 8);

        static int RowLength(int colors, int bitsPerComponent, int columns) =>
            (colors * bitsPerComponent * columns + 7) / 8;

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc)
            {
                return a;
            }

            return pb <= pc ? b : c;
        }
    }
}

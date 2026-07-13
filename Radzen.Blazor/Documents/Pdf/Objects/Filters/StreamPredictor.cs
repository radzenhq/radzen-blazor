namespace Radzen.Documents.Pdf.Objects.Filters;

/// <summary>
/// Applies the optional <c>/DecodeParms</c> predictor (PNG or TIFF) that may follow a
/// FlateDecode or LZWDecode link. A predictor of 1 (or an absent parameters dictionary)
/// is a no-op.
/// </summary>
internal static class StreamPredictor
{
    public static byte[] Apply(byte[] data, DictionaryObject? parms)
    {
        if (parms is null)
        {
            return data;
        }

        var predictor = ParmInt(parms, "Predictor", 1);
        if (predictor <= 1)
        {
            return data;
        }

        var columns = ParmInt(parms, "Columns", 1);
        var colors = ParmInt(parms, "Colors", 1);
        var bits = ParmInt(parms, "BitsPerComponent", 8);
        if (predictor >= 10)
        {
            return PngPredictor.Decode(data, colors, bits, columns);
        }

        return predictor == 2 ? TiffPredictor.Decode(data, colors, bits, columns) : data;
    }

    public static int ParmInt(DictionaryObject parms, string key, int fallback)
        => parms.TryGetValue(key, out var value) && value is NumberObject number ? number.IntValue : fallback;
}

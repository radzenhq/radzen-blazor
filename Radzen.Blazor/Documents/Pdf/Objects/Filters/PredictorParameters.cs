namespace Radzen.Documents.Pdf.Objects.Filters;

// /DecodeParms is attacker-controlled, so both predictors validate colors/columns before any
// of it reaches a length computation. The cap on colors is what keeps colors*columns (and,
// for PNG, colors*bitsPerComponent*columns) from overflowing once widened to long.
internal static class PredictorParameters
{
    public const int MaxColors = 32;

    public static void ValidateColorsAndColumns(int colors, int columns, string predictor)
    {
        if (colors <= 0 || colors > MaxColors || columns <= 0)
        {
            throw new DocumentParseException($"{predictor} predictor colors/columns are out of range.");
        }
    }
}

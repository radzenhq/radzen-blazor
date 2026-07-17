namespace Radzen.Documents.Pdf.Objects.Filters;

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

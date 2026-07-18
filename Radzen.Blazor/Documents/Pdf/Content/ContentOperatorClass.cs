namespace Radzen.Documents.Pdf.Content;

internal static class ContentOperatorClass
{
    public static bool IsPathConstruction(string? op) => op is
        "m" or "l" or "c" or "v" or "y" or "re" or "h" or "W" or "W*";

    public static bool IsPathPainting(string? op) => op is
        "S" or "s" or "f" or "F" or "f*" or "B" or "B*" or "b" or "b*" or "n";

    public static bool IsStateOperator(string? op) => op is
        "q" or "Q" or "cm" or "w" or "rg" or "RG" or "g" or "G" or "k" or "K"
        or "cs" or "CS" or "scn" or "sc" or "SCN" or "SC" or "d" or "BT" or "ET"
        or "Tf" or "TL" or "TD" or "Td" or "Tm" or "T*" or "Tc" or "Tw" or "Tz"
        or "Ts" or "Tr" or "BDC" or "BMC" or "EMC";

    public static bool IsElementProducing(string? op) =>
        ContentShows.IsShow(op) || op == "Do" || IsPathConstruction(op) || IsPathPainting(op);

    public static bool IsUnknown(string? op) => !IsElementProducing(op) && !IsStateOperator(op);
}

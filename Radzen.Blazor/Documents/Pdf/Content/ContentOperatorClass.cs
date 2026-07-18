using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentOperatorClass
{
    private static readonly HashSet<string> Operators =
    [
        "q", "Q", "cm", "w", "J", "j", "M", "d", "ri", "i", "gs",
        "m", "l", "c", "v", "y", "h", "re",
        "S", "s", "f", "F", "f*", "B", "B*", "b", "b*", "n", "W", "W*",
        "BT", "ET", "Td", "TD", "Tm", "T*", "Tc", "Tw", "Tz", "TL", "Tf", "Tr", "Ts", "Tj", "TJ", "'", "\"",
        "d0", "d1", "CS", "cs", "SC", "SCN", "sc", "scn", "G", "g", "RG", "rg", "K", "k",
        "sh", "Do", "BI", "MP", "DP", "BMC", "BDC", "EMC", "BX", "EX",
    ];

    public static bool IsContentOperator(string op) => Operators.Contains(op);

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

    public static bool MayPaintUnknown(string op) => op is not (
        "gs" or "ri" or "i" or "j" or "J" or "M" or "BX" or "EX" or "MP" or "DP" or "d0" or "d1");
}

using System.Globalization;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal static class PdfColor
{
    public static double Component(byte channel) => channel / 255.0;

    public static ArrayObject Rgb(Color color) =>
    [
        new NumberObject(Component(color.R)),
        new NumberObject(Component(color.G)),
        new NumberObject(Component(color.B)),
    ];

    public static string RgbOperator(Color color, string operatorName) => string.Create(
        CultureInfo.InvariantCulture,
        $"{Component(color.R):0.###} {Component(color.G):0.###} {Component(color.B):0.###} {operatorName}");
}

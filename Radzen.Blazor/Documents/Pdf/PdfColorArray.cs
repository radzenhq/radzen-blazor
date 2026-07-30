using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal static class PdfColorArray
{
    public static ArrayObject Rgb(Color color) =>
    [
        new NumberObject(color.R / 255.0),
        new NumberObject(color.G / 255.0),
        new NumberObject(color.B / 255.0),
    ];
}

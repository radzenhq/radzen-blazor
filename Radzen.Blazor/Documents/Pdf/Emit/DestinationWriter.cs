using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

internal static class DestinationWriter
{
    public static ArrayObject Write(OutlineTarget target, ReferenceObject page, ArrayObject fallback)
    {
        var arguments = target.FitArguments;
        return target.Fit switch
        {
            OutlineFit.Fit => [page, new NameObject("Fit")],
            OutlineFit.FitHorizontal => [page, new NameObject("FitH"), new NumberObject(arguments[0])],
            OutlineFit.FitVertical => [page, new NameObject("FitV"), new NumberObject(arguments[0])],
            OutlineFit.FitBounding => [page, new NameObject("FitB")],
            OutlineFit.FitBoundingHorizontal => [page, new NameObject("FitBH"), new NumberObject(arguments[0])],
            OutlineFit.FitBoundingVertical => [page, new NameObject("FitBV"), new NumberObject(arguments[0])],
            OutlineFit.Rectangle =>
            [
                page,
                new NameObject("FitR"),
                new NumberObject(arguments[0]),
                new NumberObject(arguments[1]),
                new NumberObject(arguments[2]),
                new NumberObject(arguments[3]),
            ],
            OutlineFit.Coordinates =>
            [
                page,
                new NameObject("XYZ"),
                new NumberObject(arguments[0]),
                new NumberObject(arguments[1]),
                new NumberObject(arguments[2]),
            ],
            _ => fallback,
        };
    }
}

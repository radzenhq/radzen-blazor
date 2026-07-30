using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf;

internal readonly struct RectPolicy
{
    private RectPolicy(string? missingMessage, string? nonNumericMessage, double fallbackWidth, double fallbackHeight, bool rejectNonNumeric)
    {
        MissingMessage = missingMessage;
        NonNumericMessage = nonNumericMessage;
        FallbackWidth = fallbackWidth;
        FallbackHeight = fallbackHeight;
        RejectNonNumeric = rejectNonNumeric;
    }

    public string? MissingMessage { get; }

    public string? NonNumericMessage { get; }

    public double FallbackWidth { get; }

    public double FallbackHeight { get; }

    public bool RejectNonNumeric { get; }

    public bool Throws => MissingMessage is not null;

    public static RectPolicy Strict(string missingMessage, string nonNumericMessage)
        => new(missingMessage, nonNumericMessage, 0, 0, false);

    public static RectPolicy ZeroFallback { get; } = new(null, null, 0, 0, false);

    public static RectPolicy DefaultSize(double width, double height) => new(null, null, width, height, false);

    public static RectPolicy Rejecting { get; } = new(null, null, 0, 0, true);
}

internal static class RectReader
{
    internal static PdfRect Read(DocumentReader reader, ArrayObject? value, RectPolicy policy)
        => ResolveCorners(reader, value, policy) is { } corners
            ? PdfRect.Normalize(corners)
            : PdfRect.FromSize(0, 0, policy.FallbackWidth, policy.FallbackHeight);

    // /Rect corners may be in either order and may be indirect references (ISO 32000-1 7.9.5).
    internal static double[]? ResolveCorners(DocumentReader reader, ArrayObject? value, RectPolicy policy)
    {
        if (value is null || value.Count < 4 || (policy.Throws && value.Count != 4))
        {
            return policy.Throws
                ? throw new DocumentParseException(policy.MissingMessage!, -1)
                : null;
        }

        var corners = new double[4];
        for (var i = 0; i < corners.Length; i++)
        {
            switch (reader.AsNumber(value[i]))
            {
                case { } number:
                    corners[i] = number;
                    break;
                case null when policy.Throws:
                    throw new DocumentParseException(policy.NonNumericMessage!, -1);
                case null when policy.RejectNonNumeric:
                    return null;
                default:
                    corners[i] = 0.0;
                    break;
            }
        }

        return corners;
    }
}

namespace Radzen.Documents.Pdf;


// ISO 32000-1 8.6.5.8: the ri operator and the /Intent entry.
internal enum RenderingIntent
{
    AbsoluteColorimetric,

    RelativeColorimetric,

    Saturation,

    Perceptual,
}

internal static class RenderingIntents
{
    public static string PdfName(this RenderingIntent intent) => intent switch
    {
        RenderingIntent.AbsoluteColorimetric => "AbsoluteColorimetric",
        RenderingIntent.RelativeColorimetric => "RelativeColorimetric",
        RenderingIntent.Saturation => "Saturation",
        _ => "Perceptual",
    };
}

namespace Radzen.Documents.Pdf.Emit;

internal static class PageSpace
{
    public static Matrix Flip(in Matrix transform, double pageHeight)
        => Matrix.FromComponents(
            transform.A,
            -transform.B,
            -transform.C,
            transform.D,
            transform.E + (pageHeight * transform.C),
            pageHeight - transform.F - (pageHeight * transform.D));
}
